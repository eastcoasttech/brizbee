using Brizbee.Common.Models;
using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml;

namespace Brizbee.Integration.Utility.ViewModels.InventoryConsumptions
{
    public class SyncViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public bool IsExitEnabled { get; set; }
        public bool IsTryEnabled { get; set; }
        public bool IsStartOverEnabled { get; set; }
        public string StatusText { get; set; }
        public int AddErrorCount { get; set; }
        public int ValidationErrorCount { get; set; }
        public int SaveErrorCount { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Private Fields
        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        #endregion

        public void Sync()
        {
            // Disable the buttons
            IsExitEnabled = false;
            IsTryEnabled = false;
            IsStartOverEnabled = false;
            OnPropertyChanged("IsExitEnabled");
            OnPropertyChanged("IsTryEnabled");
            OnPropertyChanged("IsStartOverEnabled");

            // Reset error count
            ValidationErrorCount = 0;
            SaveErrorCount = 0;

            StatusText = string.Format("{0} - Starting.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            // Requests to the QuickBooks API are made in QBXML format
            var doc = new XmlDocument();

            // Add the prolog processing instructions
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"13.0\""));

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            StatusText += string.Format("{0} - Connecting to QuickBooks.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            try
            {
                var req = new RequestProcessor2();
                req.OpenConnection2("", "BRIZBEE Integration Utility", QBXMLRPConnectionType.localQBD);
                var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);

                StatusText += string.Format("{0} - Syncing.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                var inventoryService = new InventoryService();

                // Build the request to get unsynced adjustments
                var httpRequest = new RestRequest("api/QBDInventoryConsumptions/Unsynced", Method.GET);

                // Execute request
                var httpResponse = client.Execute<List<QBDInventoryConsumption>>(httpRequest);
                if ((httpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (httpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                {
                    var consumptions = httpResponse.Data;

                    // Build the request to store adjustments as sales receipts
                    inventoryService.BuildSalesReceiptAddRq(doc, inner, consumptions);

                    var response = req.ProcessRequest(ticket, doc.OuterXml);

                    // Then walk the response
                    var walkReponse = inventoryService.WalkSalesReceiptAddRs(response);

                    if (SaveErrorCount > 0)
                    {
                        StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                        OnPropertyChanged("StatusText");

                        // Enable the buttons
                        IsExitEnabled = true;
                        IsTryEnabled = true;
                        IsStartOverEnabled = true;
                        OnPropertyChanged("IsExitEnabled");
                        OnPropertyChanged("IsTryEnabled");
                        OnPropertyChanged("IsStartOverEnabled");
                    }
                    else
                    {
                        StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                        OnPropertyChanged("StatusText");

                        // Enable the buttons
                        IsExitEnabled = true;
                        IsStartOverEnabled = true;
                        OnPropertyChanged("IsExitEnabled");
                        OnPropertyChanged("IsStartOverEnabled");
                    }
                }
                else
                {

                }

                // Close the QuickBooks connection
                req.EndSession(ticket);
                req.CloseConnection();
                req = null;
            }
            catch (COMException cex)
            {
                // Enable the buttons
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");

                if ((uint)cex.ErrorCode == 0x80040408)
                {
                    StatusText += string.Format("{0} - Sync failed. QuickBooks Desktop is not open.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");

                    // Bubbles exception up to user interface
                    throw new Exception("You must open QuickBooks Desktop before you can sync.");
                }
                else
                {
                    StatusText += string.Format("{0} - Sync failed. {1}\r\n", DateTime.Now.ToString(), cex.Message);
                    OnPropertyChanged("StatusText");

                    // Bubbles exception up to user interface
                    throw;
                }
            }
            catch (Exception ex)
            {
                StatusText += string.Format("{0} - Sync failed. {1}\r\n", DateTime.Now.ToString(), ex.Message);
                OnPropertyChanged("StatusText");

                // Enable the buttons
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");

                // Bubbles exception up to user interface
                throw;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
