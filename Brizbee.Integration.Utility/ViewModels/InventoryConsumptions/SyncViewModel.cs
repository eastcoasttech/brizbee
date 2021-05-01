using Brizbee.Common.Models;
using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        private string selectedMethod = Application.Current.Properties["SelectedMethod"] as string;
        private string selectedValue = Application.Current.Properties["SelectedValue"] as string;
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
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"14.0\""));

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            StatusText += string.Format("{0} - Connecting to QuickBooks.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            // QBXML request processor must always be closed after use.
            var req = new RequestProcessor2();

            try
            {
                req.OpenConnection2("", "BRIZBEE Integration Utility", QBXMLRPConnectionType.localQBD);
                var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);

                StatusText += string.Format("{0} - Syncing.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                if (selectedMethod == "Inventory Adjustment")
                {
                    StatusText += string.Format("{0} - Using {1} method.\r\n", DateTime.Now.ToString(), selectedMethod);
                    OnPropertyChanged("StatusText");
                }
                else if (selectedMethod == "Sales Receipt")
                {
                    StatusText += string.Format("{0} - Using {1} method and {2} value.\r\n", DateTime.Now.ToString(), selectedMethod, selectedValue);
                    OnPropertyChanged("StatusText");
                }

                var inventoryService = new InventoryService();

                // Build the request to get unsynced consumption
                var unsyncedHttpRequest = new RestRequest("api/QBDInventoryConsumptions/Unsynced", Method.GET);
                unsyncedHttpRequest.AddHeader("X-Paging-PageNumber", "1");
                unsyncedHttpRequest.AddHeader("X-Paging-PageSize", "1000");

                // Execute request
                var unsyncedHttpResponse = client.Execute<List<QBDInventoryConsumption>>(unsyncedHttpRequest);
                if ((unsyncedHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (unsyncedHttpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                {
                    var consumptions = unsyncedHttpResponse.Data;

                    if (consumptions.Count == 0)
                    {
                        StatusText += string.Format("{0} - There is no inventory consumption to sync.\r\n", DateTime.Now.ToString());
                        OnPropertyChanged("StatusText");
                    }
                    else
                    {
                        // Build the request to store consumptions.
                        if (selectedMethod == "Sales Receipt")
                        {
                            inventoryService.BuildSalesReceiptAddRq(doc, inner, consumptions, selectedValue.ToUpperInvariant());
                        }
                        else if (selectedMethod == "Inventory Adjustment")
                        {
                            inventoryService.BuildInventoryAdjustmentAddRq(doc, inner, consumptions, selectedValue.ToUpperInvariant());
                        }

                        Trace.TraceInformation(doc.OuterXml);

                        // Make the request.
                        var response = req.ProcessRequest(ticket, doc.OuterXml);

                        Trace.TraceInformation(response);

                        // Then walk the response
                        if (selectedMethod == "Sales Receipt")
                        {
                            var walkReponse = inventoryService.WalkSalesReceiptAddRs(response);
                        }
                        else if (selectedMethod == "Inventory Adjustment")
                        {
                            var walkReponse = inventoryService.WalkInventoryAdjustmentAddRs(response);
                        }

                        if (SaveErrorCount > 0)
                        {
                            StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                            OnPropertyChanged("StatusText");
                        }
                        else
                        {
                            // Build the request to send the sync details
                            var syncHttpRequest = new RestRequest("api/QBDInventoryConsumptions/Sync", Method.POST);
                            syncHttpRequest.AddJsonBody(consumptions.Select(c => c.Id).ToArray());

                            // Execute request
                            var syncHttpResponse = client.Execute(syncHttpRequest);
                            if ((syncHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                                    (syncHttpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                            {
                                StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                                OnPropertyChanged("StatusText");
                            }
                            else
                            {
                                StatusText += $"{DateTime.Now} - {syncHttpResponse.Content}";
                                StatusText += string.Format("{0} - Sync failed.\r\n", DateTime.Now.ToString());
                                OnPropertyChanged("StatusText");
                            }
                        }
                    }
                }
                else
                {
                    StatusText += $"{DateTime.Now} - {unsyncedHttpResponse.Content}";
                    StatusText += string.Format("{0} - Sync failed.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }

                // Close the QuickBooks connection
                req.EndSession(ticket);
                req.CloseConnection();
                req = null;
            }
            catch (COMException cex)
            {
                Trace.TraceError(cex.ToString());

                if ((uint)cex.ErrorCode == 0x80040408)
                {
                    StatusText += string.Format("{0} - Sync failed. QuickBooks Desktop is not open.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += string.Format("{0} - Sync failed. {1}\r\n", DateTime.Now.ToString(), cex.Message);
                    OnPropertyChanged("StatusText");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                StatusText += string.Format("{0} - Sync failed. {1}\r\n", DateTime.Now.ToString(), ex.Message);
                OnPropertyChanged("StatusText");
            }
            finally
            {
                // Enable the buttons.
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");

                // Try to close the QuickBooks connection if it is still open.
                if (req != null)
                {
                    req.CloseConnection();
                    req = null;
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
