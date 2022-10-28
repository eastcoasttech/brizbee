//
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
using Brizbee.Integration.Utility.Exceptions;
using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.InventoryConsumptions
{
    public class SyncViewModel : INotifyPropertyChanged
    {
        public bool IsExitEnabled { get; set; }

        public bool IsTryEnabled { get; set; }

        public bool IsStartOverEnabled { get; set; }

        public string StatusText { get; set; }

        public int ValidationErrorCount { get; set; }

        public int SaveErrorCount { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly RestClient _client = Application.Current.Properties["Client"] as RestClient;
        private readonly string _selectedMethod = Application.Current.Properties["SelectedMethod"] as string;
        private readonly string _selectedValue = Application.Current.Properties["SelectedValue"] as string;
        private readonly string _vendorFullName = ConfigurationManager.AppSettings["InventoryConsumptionVendorName"];
        private const string ReferenceNumber = "Materials";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
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

            StatusText = $"{DateTime.Now} - Starting.\r\n";
            OnPropertyChanged("StatusText");


            // ------------------------------------------------------------
            // Ensure the vendor name is specified.
            // ------------------------------------------------------------

            if (string.IsNullOrEmpty(_vendorFullName))
            {
                StatusText =
                    $"{DateTime.Now} - Must specify the InventoryConsumptionVendorName in the configuration.\r\n";
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

            StatusText += $"{DateTime.Now} - Connecting to QuickBooks.\r\n";
            OnPropertyChanged("StatusText");

            // QBXML request processor must always be closed after use.
            var req = new RequestProcessor2();

            try
            {
                req.OpenConnection2("", "BRIZBEE Integration Utility", QBXMLRPConnectionType.localQBD);
                var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
                var companyFileName = req.GetCurrentCompanyFileName(ticket);

                StatusText += $"{DateTime.Now} - Syncing.\r\n";
                OnPropertyChanged("StatusText");

                // Determine the method of recording consumption.
                if (_selectedMethod == "Inventory Adjustment")
                {
                    StatusText += $"{DateTime.Now} - Using {_selectedMethod} method.\r\n";
                    OnPropertyChanged("StatusText");
                }
                else if (_selectedMethod == "Sales Receipt")
                {
                    StatusText +=
                        $"{DateTime.Now} - Using {_selectedMethod} method and {_selectedValue} value.\r\n";
                    OnPropertyChanged("StatusText");
                }
                else if (_selectedMethod == "Bill")
                {
                    StatusText +=
                        $"{DateTime.Now} - Using {_selectedMethod} method and {_selectedValue} value with {_vendorFullName} vendor.\r\n";
                    OnPropertyChanged("StatusText");
                }


                // ------------------------------------------------------------
                // Collect the QuickBooks host details.
                // ------------------------------------------------------------

                // Prepare a new QBXML document.
                var hostQbxml = service.MakeQBXMLDocument();
                var hostDocument = hostQbxml.Item1;
                var hostElement = hostQbxml.Item2;
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
                    StatusText += $"{DateTime.Now} - There is no inventory consumption to sync.\r\n";
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    // ------------------------------------------------------------
                    // Record the unsynced consumptions.
                    // ------------------------------------------------------------

                    // Build the request to store consumptions.
                    if (_selectedMethod == "Sales Receipt")
                    {
                        // Prepare a new QBXML document.
                        var (consDocument, consElement) = service.MakeQBXMLDocument();

                        service.BuildSalesReceiptAddRq(consDocument, consElement, consumptions, _selectedValue.ToUpperInvariant());

                        // Make the request.
                        Logger.Debug(consDocument.OuterXml);
                        var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                        Logger.Debug(consResponse);

                        // Then walk the response.
                        var (_, _, transactionIds) = service.WalkSalesReceiptAddRs(consResponse);
                        txnIds = txnIds.Concat(transactionIds).ToList();
                    }
                    else if (_selectedMethod == "Bill")
                    {
                        foreach (var consumption in consumptions)
                        {
                            // Prepare a new QBXML document.
                            var (consDocument, consElement) = service.MakeQBXMLDocument();

                            service.BuildBillAddRq(consDocument, consElement, consumption, _vendorFullName, ReferenceNumber, _selectedValue.ToUpperInvariant());

                            // Make the request.
                            Logger.Debug(consDocument.OuterXml);
                            var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                            Logger.Debug(consResponse);

                            // Then walk the response.
                            var (_, _, transactionIds) = service.WalkBillAddRs(consResponse);
                            txnIds = txnIds.Concat(transactionIds).ToList();
                        }
                    }
                    else if (_selectedMethod == "Inventory Adjustment")
                    {
                        // Prepare a new QBXML document.
                        var (consDocument, consElement) = service.MakeQBXMLDocument();

                        service.BuildInventoryAdjustmentAddRq(consDocument, consElement, consumptions, _selectedValue.ToUpperInvariant());

                        // Make the request.
                        Logger.Debug(consDocument.OuterXml);
                        var consResponse = req.ProcessRequest(ticket, consDocument.OuterXml);
                        Logger.Debug(consResponse);

                        // Then walk the response.
                        var (_, _, transactionIds) = service.WalkInventoryAdjustmentAddRs(consResponse);
                        txnIds = txnIds.Concat(transactionIds).ToList();
                    }
                    
                    var transactionType = string.Empty;

                    switch (_selectedMethod)
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

                    if (SaveErrorCount > 0)
                    {
                        StatusText +=
                            $"{DateTime.Now} - Sync failed. Please correct the {SaveErrorCount} errors first.\r\n";
                        OnPropertyChanged("StatusText");
                        
                        // Reverse the transactions.
                        foreach (var txnId in txnIds)
                        {
                            // Prepare a new QBXML document.
                            var (delDocument, delElement) = service.MakeQBXMLDocument();
                            service.BuildTxnDelRq(delDocument, delElement, transactionType, txnId); // InventoryAdjustment, SalesReceipt, or Bill

                            // Make the request.
                            Logger.Debug(delDocument.OuterXml);
                            var delResponse = req.ProcessRequest(ticket, delDocument.OuterXml);
                            Logger.Debug(delResponse);
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
                        var syncHttpRequest = new RestRequest("api/QBDInventoryConsumptions/Sync", Method.Post);
                        syncHttpRequest.AddJsonBody(payload);
                        syncHttpRequest.AddQueryParameter("recordingMethod", _selectedMethod);
                        syncHttpRequest.AddQueryParameter("valueMethod", _selectedValue);
                        syncHttpRequest.AddQueryParameter("productName", hostDetails.QBProductName);
                        syncHttpRequest.AddQueryParameter("majorVersion", hostDetails.QBMajorVersion);
                        syncHttpRequest.AddQueryParameter("minorVersion", hostDetails.QBMinorVersion);
                        syncHttpRequest.AddQueryParameter("country", hostDetails.QBCountry);
                        syncHttpRequest.AddQueryParameter("supportedQBXMLVersion", hostDetails.QBSupportedQBXMLVersions);
                        syncHttpRequest.AddQueryParameter("hostname", Environment.MachineName);
                        syncHttpRequest.AddQueryParameter("companyFilePath", companyFileName);

                        // Execute request.
                        var syncHttpResponse = _client.Execute(syncHttpRequest);
                        if ((syncHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                                (syncHttpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                        {
                            StatusText += $"{DateTime.Now} - Synced Successfully.\r\n";
                            OnPropertyChanged("StatusText");
                        }
                        else
                        {
                            // Reverse the transactions.
                            foreach (var txnId in txnIds)
                            {
                                // Prepare a new QBXML document.
                                var (delDocument, delElement) = service.MakeQBXMLDocument();
                                service.BuildTxnDelRq(delDocument, delElement, transactionType, txnId); // InventoryAdjustment, SalesReceipt, or Bill

                                // Make the request.
                                Logger.Debug(delDocument.OuterXml);
                                var delResponse = req.ProcessRequest(ticket, delDocument.OuterXml);
                                Logger.Debug(delResponse);
                            }

                            StatusText += $"{DateTime.Now} - {syncHttpResponse.Content}";
                            StatusText += $"{DateTime.Now} - Sync failed.\r\n";
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
                StatusText +=
                    $"{DateTime.Now} - Sync failed. Could not download consumption from the server.\r\n";
                OnPropertyChanged("StatusText");
            }
            catch (COMException cex)
            {
                Logger.Error(cex.ToString());

                if ((uint)cex.ErrorCode == 0x80040408)
                {
                    StatusText += $"{DateTime.Now} - Sync failed. QuickBooks Desktop is not open.\r\n";
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += $"{DateTime.Now} - Sync failed. {cex.Message}\r\n";
                    OnPropertyChanged("StatusText");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                StatusText += $"{DateTime.Now} - Sync failed. {ex.Message}\r\n";
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
            StatusText += $"{DateTime.Now} - Getting unsynced consumption.\r\n";
            OnPropertyChanged("StatusText");

            // Build the request to get unsynced consumption.
            var request = new RestRequest("api/QBDInventoryConsumptions/Unsynced");
            request.AddHeader("X-Paging-PageNumber", "1");
            request.AddHeader("X-Paging-PageSize", "1000");

            // Execute request.
            var response = _client.Execute<List<QBDInventoryConsumption>>(request);
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
