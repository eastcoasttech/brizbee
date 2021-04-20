using Brizbee.Common.Models;
using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml;

namespace Brizbee.Integration.Utility.ViewModels.InventoryItems
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

            StatusText += string.Format("{0} - Connecting to QuickBooks.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            try
            {
                var req = new RequestProcessor2();
                req.OpenConnection2("", "BRIZBEE Integration Utility", QBXMLRPConnectionType.localQBD);
                var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);

                StatusText += string.Format("{0} - Syncing.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                var service = new InventoryService();
                var items = new List<QBDInventoryItem>();
                var sites = new List<QBDInventorySite>();
                var units = new List<QBDUnitOfMeasureSet>();

                StatusText += string.Format("{0} - Syncing inventory items.\r\n", DateTime.Now.ToString());
                items = SyncInventoryItems(service, ticket, req);

                // Attempt to sync sites even if they are not enabled or available
                StatusText += string.Format("{0} - Syncing inventory sites.\r\n", DateTime.Now.ToString());
                sites = SyncInventorySites(service, ticket, req);

                // Attempt to sync sites even if they are not enabled or available
                StatusText += string.Format("{0} - Syncing unit of measure sets.\r\n", DateTime.Now.ToString());
                units = SyncUnitOfMeasureSets(service, ticket, req);

                // Build the request to send the sync details
                var httpRequest = new RestRequest("api/QBDInventoryItems/Sync", Method.POST);
                httpRequest.AddJsonBody(new
                {
                    InventoryItems = items,
                    InventorySites = sites,
                    UnitOfMeasureSets = units
                });

                // Execute request
                var httpResponse = client.Execute(httpRequest);
                if ((httpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (httpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                {
                    StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += $"{DateTime.Now} - {httpResponse.Content}";
                    StatusText += string.Format("{0} - Sync failed.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }

                // Close the QuickBooks connection
                req.EndSession(ticket);
                req.CloseConnection();
                req = null;

                // Enable the buttons
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");
            }
            catch (COMException cex)
            {
                Trace.TraceError(cex.ToString());

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
                    //throw;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

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
                //throw;
            }
        }

        private List<QBDInventoryItem> SyncInventoryItems(InventoryService service, string ticket, RequestProcessor2 req)
        {
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

            // Build the request to get inventory items
            service.BuildInventoryItemQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                // Then walk the response
                var walkReponse = service.WalkInventoryItemQueryRs(response);

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

                    return new List<QBDInventoryItem>();
                }
                else
                {
                    return walkReponse.Item3;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return new List<QBDInventoryItem>();
            }
        }

        private List<QBDInventorySite> SyncInventorySites(InventoryService service, string ticket, RequestProcessor2 req)
        {
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

            // Build the request to get inventory sites
            service.BuildInventorySiteQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                Trace.TraceInformation(response);

                if (string.IsNullOrEmpty(response))
                    return new List<QBDInventorySite>();

                // Then walk the response
                var walkReponse = service.WalkInventorySiteQueryRs(response);

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

                    return new List<QBDInventorySite>();
                }
                else
                {
                    return walkReponse.Item3;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return new List<QBDInventorySite>();
            }
        }

        private List<QBDUnitOfMeasureSet> SyncUnitOfMeasureSets(InventoryService service, string ticket, RequestProcessor2 req)
        {
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

            // Build the request to get unit of measure sets
            service.BuildUnitOfMeasureSetQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                Trace.TraceInformation(response);

                if (string.IsNullOrEmpty(response))
                    return new List<QBDUnitOfMeasureSet>();

                // Then walk the response
                var walkReponse = service.WalkUnitOfMeasureSetQueryRs(response);

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

                    return new List<QBDUnitOfMeasureSet>();
                }
                else
                {
                    return walkReponse.Item3;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return new List<QBDUnitOfMeasureSet>();
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
