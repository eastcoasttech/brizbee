//
//  SyncViewModel.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Database Management.
//
//  This program is free software: you can redistribute
//  it and/or modify it under the terms of the GNU General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will
//  be useful, but WITHOUT ANY WARRANTY; without even the implied
//  warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//  See the GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.
//  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Models;
using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using Newtonsoft.Json;
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
            // Disable the buttons.
            IsExitEnabled = false;
            IsTryEnabled = false;
            IsStartOverEnabled = false;
            OnPropertyChanged("IsExitEnabled");
            OnPropertyChanged("IsTryEnabled");
            OnPropertyChanged("IsStartOverEnabled");

            // Reset error count.
            ValidationErrorCount = 0;
            SaveErrorCount = 0;

            StatusText = string.Format("{0} - Starting.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            var service = new QuickBooksService();

            StatusText += string.Format("{0} - Connecting to QuickBooks.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            // QBXML request processor must always be closed after use.
            var req = new RequestProcessor2();

            try
            {
                req.OpenConnection2("", "BRIZBEE Integration Utility", QBXMLRPConnectionType.localQBD);
                var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
                var companyFileName = req.GetCurrentCompanyFileName(ticket);

                StatusText += string.Format("{0} - Syncing.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                // ------------------------------------------------------------
                // Collect the QuickBooks host details.
                // ------------------------------------------------------------

                // Prepare a new QBXML document.
                var hostQBXML = service.MakeQBXMLDocument();
                var hostDocument = hostQBXML.Item1;
                var hostElement = hostQBXML.Item2;
                service.BuildHostQueryRq(hostDocument, hostElement);

                // Make the request.
                var hostResponse = req.ProcessRequest(ticket, hostDocument.OuterXml);

                // Then walk the response.
                var hostWalkResponse = service.WalkHostQueryRsAndParseHostDetails(hostResponse);
                var hostDetails = hostWalkResponse.Item3;

                // ------------------------------------------------------------
                // Collect the inventory items.
                // ------------------------------------------------------------

                var items = new List<QBDInventoryItem>();

                StatusText += string.Format("{0} - Collecting inventory items.\r\n", DateTime.Now.ToString());
                items = SyncInventoryItems(service, ticket, req);

                // Cannot continue to sync if there are no items.
                if (items.Count == 0)
                    throw new Exception("There are no inventory items to sync.");

                // ------------------------------------------------------------
                // Collect the inventory sites.
                // ------------------------------------------------------------

                var sites = new List<QBDInventorySite>();

                // Attempt to sync sites even if they are not enabled or available.
                StatusText += string.Format("{0} - Collecting inventory sites.\r\n", DateTime.Now.ToString());
                sites = SyncInventorySites(service, ticket, req);

                // ------------------------------------------------------------
                // Collect the units of measure.
                // ------------------------------------------------------------

                var units = new List<QBDUnitOfMeasureSet>();

                // Attempt to sync sites even if they are not enabled or available.
                StatusText += string.Format("{0} - Collecting unit of measure sets.\r\n", DateTime.Now.ToString());
                units = SyncUnitOfMeasureSets(service, ticket, req);

                // ------------------------------------------------------------
                // Send the items to the server.
                // ------------------------------------------------------------

                // Build the payload.
                var payload = new
                {
                    InventoryItems = items ?? new List<QBDInventoryItem>(),
                    InventorySites = sites ?? new List<QBDInventorySite>(),
                    UnitOfMeasureSets = units ?? new List<QBDUnitOfMeasureSet>()
                };

                // Build the request to send the sync details.
                var syncHttpRequest = new RestRequest("api/QBDInventoryItems/Sync", Method.POST);
                syncHttpRequest.AddJsonBody(payload);
                syncHttpRequest.AddQueryParameter("productName", hostDetails.QBProductName);
                syncHttpRequest.AddQueryParameter("majorVersion", hostDetails.QBMajorVersion);
                syncHttpRequest.AddQueryParameter("minorVersion", hostDetails.QBMinorVersion);
                syncHttpRequest.AddQueryParameter("country", hostDetails.QBCountry);
                syncHttpRequest.AddQueryParameter("supportedQBXMLVersion", hostDetails.QBSupportedQBXMLVersions);
                syncHttpRequest.AddQueryParameter("hostname", Environment.MachineName);
                syncHttpRequest.AddQueryParameter("companyFilePath", companyFileName);

                // Execute request.
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

                // Close the QuickBooks connection.
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

        private List<QBDInventoryItem> SyncInventoryItems(QuickBooksService service, string ticket, RequestProcessor2 req)
        {
            // Requests to the QuickBooks API are made in QBXML format.
            var doc = new XmlDocument();

            // Add the prolog processing instructions.
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"14.0\""));

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            // Build the request to get inventory items.
            service.BuildInventoryItemQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                Trace.TraceInformation(response);

                // Then walk the response.
                var walkReponse = service.WalkInventoryItemQueryRs(response);

                if (SaveErrorCount > 0)
                {
                    StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                    OnPropertyChanged("StatusText");

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

        private List<QBDInventorySite> SyncInventorySites(QuickBooksService service, string ticket, RequestProcessor2 req)
        {
            // Requests to the QuickBooks API are made in QBXML format.
            var doc = new XmlDocument();

            // Add the prolog processing instructions.
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"14.0\""));

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            // Build the request to get inventory sites.
            service.BuildInventorySiteQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                Trace.TraceInformation(response);

                if (string.IsNullOrEmpty(response))
                    return new List<QBDInventorySite>();

                // Then walk the response.
                var walkReponse = service.WalkInventorySiteQueryRs(response);

                if (SaveErrorCount > 0)
                {
                    StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                    OnPropertyChanged("StatusText");

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

        private List<QBDUnitOfMeasureSet> SyncUnitOfMeasureSets(QuickBooksService service, string ticket, RequestProcessor2 req)
        {
            // Requests to the QuickBooks API are made in QBXML format.
            var doc = new XmlDocument();

            // Add the prolog processing instructions.
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"14.0\""));

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            // Build the request to get unit of measure sets.
            service.BuildUnitOfMeasureSetQueryRq(doc, inner);

            try
            {
                Trace.TraceInformation(doc.OuterXml);

                var response = req.ProcessRequest(ticket, doc.OuterXml);

                Trace.TraceInformation(response);

                if (string.IsNullOrEmpty(response))
                    return new List<QBDUnitOfMeasureSet>();

                // Then walk the response.
                var walkReponse = service.WalkUnitOfMeasureSetQueryRs(response);

                if (SaveErrorCount > 0)
                {
                    StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                    OnPropertyChanged("StatusText");

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
