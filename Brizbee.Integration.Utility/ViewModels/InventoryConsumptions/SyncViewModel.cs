﻿//
//  SyncViewModel.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Integration Utility.
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
using Brizbee.Integration.Utility.Exceptions;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Configuration;
using NLog;

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
        private string vendorFullName = ConfigurationManager.AppSettings["InventoryConsumptionVendorName"].ToString();
        private string refNumber = "Materials";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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


            // ------------------------------------------------------------
            // Ensure the vendor name is specified.
            // ------------------------------------------------------------

            if (string.IsNullOrEmpty(vendorFullName))
            {
                StatusText = string.Format("{0} - Must specify the InventoryConsumptionVendorName in the configuration.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                // Enable the buttons.
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");

                return;
            }


            // ------------------------------------------------------------
            // Prepare the QuickBooks connection.
            // ------------------------------------------------------------

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

                // Determine the method of recording consumption.
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
                else if (selectedMethod == "Bill")
                {
                    StatusText += string.Format("{0} - Using {1} method and {2} value with {3} vendor.\r\n",
                        DateTime.Now.ToString(), selectedMethod, selectedValue, vendorFullName);
                    OnPropertyChanged("StatusText");
                }


                // ------------------------------------------------------------
                // Collect the QuickBooks host details.
                // ------------------------------------------------------------

                // Prepare a new QBXML document.
                var hostQBXML = service.MakeQBXMLDocument();
                var hostDocument = hostQBXML.Item1;
                var hostElement = hostQBXML.Item2;
                service.BuildHostQueryRq(hostDocument, hostElement);

                // Make the request.
                Logger.Debug(hostDocument.OuterXml);
                var hostResponse = req.ProcessRequest(ticket, hostDocument.OuterXml);
                Logger.Debug(hostResponse);

                // Then walk the response.
                var hostWalkResponse = service.WalkHostQueryRsAndParseHostDetails(hostResponse);
                var hostDetails = hostWalkResponse.Item3;


                // ------------------------------------------------------------
                // Collect any unsynced consumption.
                // ------------------------------------------------------------

                var consumptions = GetConsumption();
                var txnIds = new List<string>(consumptions.Count);

                if (consumptions.Count == 0)
                {
                    StatusText += string.Format("{0} - There is no inventory consumption to sync.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    // ------------------------------------------------------------
                    // Record the unsynced consumptions.
                    // ------------------------------------------------------------

                    // Build the request to store consumptions.
                    if (selectedMethod == "Sales Receipt")
                    {
                        // Prepare a new QBXML document.
                        var consQBXML = service.MakeQBXMLDocument();
                        var consDocument = consQBXML.Item1;
                        var consElement = consQBXML.Item2;

                        service.BuildSalesReceiptAddRq(consDocument, consElement, consumptions, selectedValue.ToUpperInvariant());

                        // Make the request.
                        Logger.Debug(consDocument.OuterXml);
                        var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                        Logger.Debug(consResponse);

                        // Then walk the response.
                        var walkReponse = service.WalkSalesReceiptAddRs(consResponse);
                        txnIds = walkReponse.Item3;
                    }
                    else if (selectedMethod == "Bill")
                    {
                        foreach (var consumption in consumptions)
                        {
                            // Prepare a new QBXML document.
                            var consQBXML = service.MakeQBXMLDocument();
                            var consDocument = consQBXML.Item1;
                            var consElement = consQBXML.Item2;

                            service.BuildBillAddRq(consDocument, consElement, consumption, vendorFullName, refNumber, selectedValue.ToUpperInvariant());

                            // Make the request.
                            Logger.Debug(consDocument.OuterXml);
                            var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                            Logger.Debug(consResponse);

                            // Then walk the response.
                            var walkReponse = service.WalkBillAddRs(consResponse);
                            txnIds = txnIds.Concat(walkReponse.Item3).ToList();
                        }
                    }
                    else if (selectedMethod == "Inventory Adjustment")
                    {
                        // Prepare a new QBXML document.
                        var consQBXML = service.MakeQBXMLDocument();
                        var consDocument = consQBXML.Item1;
                        var consElement = consQBXML.Item2;

                        service.BuildInventoryAdjustmentAddRq(consDocument, consElement, consumptions, selectedValue.ToUpperInvariant());

                        // Make the request.
                        Logger.Debug(consDocument.OuterXml);
                        var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                        Logger.Debug(consResponse);

                        // Then walk the response.
                        var walkReponse = service.WalkInventoryAdjustmentAddRs(consResponse);
                        txnIds = walkReponse.Item3;
                    }

                    if (SaveErrorCount > 0)
                    {
                        StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                        OnPropertyChanged("StatusText");

                        var transactionType = "";

                        switch (selectedMethod)
                        {
                            case "Sales Receipt":
                                transactionType = "SalesReceipt";
                                break;
                            case "Bill":
                                transactionType = "Bill";
                                break;
                            case "Inventory Adjustment":
                                transactionType = "InventoryAdjustment";
                                break;
                        }

                        // Reverse the transactions.
                        foreach (var txnId in txnIds)
                        {
                            // Prepare a new QBXML document.
                            var delQBXML = service.MakeQBXMLDocument();
                            var delDocument = delQBXML.Item1;
                            var delElement = delQBXML.Item2;
                            service.BuildTxnDelRq(delDocument, delElement, transactionType, txnId); // InventoryAdjustment, SalesReceipt, or Bill

                            // Make the request.
                            Logger.Debug(delDocument.OuterXml);
                            var delResponse = req.ProcessRequest(ticket, delDocument.OuterXml);
                            Logger.Debug(delResponse);

                            // Then walk the response.
                            var delWalkResponse = service.WalkTxnDelRs(delResponse);
                        }
                    }
                    else
                    {
                        // Build the payload.
                        var payload = new
                        {
                            TxnIDs = txnIds,
                            Ids = consumptions.Select(c => c.Id).ToArray()
                        };

                        // Build the request to send the sync details.
                        var syncHttpRequest = new RestRequest("api/QBDInventoryConsumptions/Sync", Method.POST);
                        syncHttpRequest.AddJsonBody(payload);
                        syncHttpRequest.AddQueryParameter("recordingMethod", selectedMethod);
                        syncHttpRequest.AddQueryParameter("valueMethod", selectedValue);
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
                    }
                }

                // Close the QuickBooks connection.
                req.EndSession(ticket);
                req.CloseConnection();
                req = null;
            }
            catch (DownloadFailedException)
            {
                StatusText += string.Format("{0} - Sync failed. Could not download consumption from the server.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");
            }
            catch (COMException cex)
            {
                Logger.Error(cex.ToString());

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
                Logger.Error(ex.ToString());

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

        private List<QBDInventoryConsumption> GetConsumption()
        {
            StatusText += string.Format("{0} - Getting unsynced consumption.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            // Build the request to get unsynced consumption.
            var request = new RestRequest("api/QBDInventoryConsumptions/Unsynced", Method.GET);
            request.AddHeader("X-Paging-PageNumber", "1");
            request.AddHeader("X-Paging-PageSize", "1000");

            // Execute request.
            var response = client.Execute<List<QBDInventoryConsumption>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                return response.Data;
            }
            else
            {
                Logger.Warn(response.Content);
                throw new DownloadFailedException();
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
