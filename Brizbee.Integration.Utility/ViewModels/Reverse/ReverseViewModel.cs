//
//  ReverseViewModel.cs
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

using Brizbee.Integration.Utility.Services;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.Reverse
{
    public class ReverseViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public bool IsExitEnabled { get; set; }
        public bool IsTryEnabled { get; set; }
        public bool IsStartOverEnabled { get; set; }
        public string StatusText { get; set; }
        public int AddErrorCount { get; set; }
        public int SaveErrorCount { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Private Fields
        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        private string transactionType = Application.Current.Properties["TransactionType"] as string;
        private string transactionId = Application.Current.Properties["TransactionId"] as string;
        #endregion

        public void Reverse()
        {
            // Disable the buttons.
            IsExitEnabled = false;
            IsTryEnabled = false;
            IsStartOverEnabled = false;
            OnPropertyChanged("IsExitEnabled");
            OnPropertyChanged("IsTryEnabled");
            OnPropertyChanged("IsStartOverEnabled");

            // Reset error count.
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

                StatusText += string.Format("{0} - Reversing.\r\n", DateTime.Now.ToString());
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
                // Reverse the transaction.
                // ------------------------------------------------------------

                // Prepare a new QBXML document.
                var delQBXML = service.MakeQBXMLDocument();
                var delDocument = delQBXML.Item1;
                var delElement = delQBXML.Item2;
                service.BuildTxnDelRq(delDocument, delElement, transactionType, transactionId); // InventoryAdjustment or SalesReceipt or TimeTracking

                // Make the request.
                var delResponse = req.ProcessRequest(ticket, delDocument.OuterXml);

                // Then walk the response.
                var delWalkResponse = service.WalkTxnDelRs(delResponse);

                // ------------------------------------------------------------
                // Send the result to the server.
                // ------------------------------------------------------------

                // Build the payload.
                var payload = new
                {
                };

                // Build the request.
                var syncHttpRequest = new RestRequest("api/QBDInventoryItems/Sync", Method.POST);
                syncHttpRequest.AddJsonBody(payload);
                syncHttpRequest.AddQueryParameter("productName", hostDetails.QBProductName);
                syncHttpRequest.AddQueryParameter("majorVersion", hostDetails.QBMajorVersion);
                syncHttpRequest.AddQueryParameter("minorVersion", hostDetails.QBMinorVersion);
                syncHttpRequest.AddQueryParameter("country", hostDetails.QBCountry);
                syncHttpRequest.AddQueryParameter("supportedQBXMLVersion", hostDetails.QBSupportedQBXMLVersions);
                syncHttpRequest.AddQueryParameter("hostname", Environment.MachineName);

                // Execute request.
                var syncHttpResponse = client.Execute(syncHttpRequest);
                if ((syncHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (syncHttpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                {
                    StatusText += string.Format("{0} - Reversed Successfully.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += $"{DateTime.Now} - {syncHttpResponse.Content}";
                    StatusText += string.Format("{0} - Reverse failed.\r\n", DateTime.Now.ToString());
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
                    StatusText += string.Format("{0} - Reverse failed. QuickBooks Desktop is not open.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += string.Format("{0} - Reverse failed. {1}\r\n", DateTime.Now.ToString(), cex.Message);
                    OnPropertyChanged("StatusText");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                StatusText += string.Format("{0} - Reverse failed. {1}\r\n", DateTime.Now.ToString(), ex.Message);
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
