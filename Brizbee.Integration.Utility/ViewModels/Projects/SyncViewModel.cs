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
using Brizbee.Integration.Utility.Serialization;
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

namespace Brizbee.Integration.Utility.ViewModels.Projects
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

                StatusText += string.Format("{0} - Syncing.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                // Get projects from the server.
                var projects = GetProjects();

                foreach (var project in projects)
                {
                    if (string.IsNullOrEmpty(project))
                        continue;

                    var split = project.Split(':');
                    var name = project; // Default to original name
                    var parentName = "";

                    // Customer and Job
                    if (split.Length > 1)
                    {
                        name = split[1];
                        parentName = split[0];

                        // Prepare a new QBXML document to find the parent and its details.
                        var findQBXML = service.MakeQBXMLDocument();
                        var findDocument = findQBXML.Item1;
                        var findElement = findQBXML.Item2;

                        // Build the request to find the parent.
                        service.BuildCustomerQueryRq(findDocument, findElement, parentName);

                        // Make the request.
                        var findResponse = req.ProcessRequest(ticket, findDocument.OuterXml);

                        // Then walk the response.
                        var findWalkResponse = service.WalkCustomerQueryRs(findResponse);

                        var found = findWalkResponse.Item3;
                        var parent = found.FirstOrDefault();

                        if (parent != null)
                        {
                            // Prepare a new QBXML document to create the job with customer details.
                            var jobQBXML = service.MakeQBXMLDocument();
                            var jobDocument = jobQBXML.Item1;
                            var jobElement = jobQBXML.Item2;

                            var newJob = parent;
                            newJob.Name = name;
                            newJob.ListId = "";

                            // Build the request to create the job.
                            service.BuildCustomerAddRqForJob(jobDocument, jobElement, newJob, parentName);

                            // Make the request.
                            var jobResponse = req.ProcessRequest(ticket, jobDocument.OuterXml);
                        }
                        else
                        {
                            // Prepare a new QBXML document to create the parent.
                            var custQBXML = service.MakeQBXMLDocument();
                            var custDocument = custQBXML.Item1;
                            var custElement = custQBXML.Item2;

                            // Build the request to create the parent.
                            service.BuildCustomerAddRqForCustomer(custDocument, custElement, parentName);

                            // Make the request.
                            var custResponse = req.ProcessRequest(ticket, custDocument.OuterXml);

                            // Prepare a new QBXML document to create the job.
                            var jobQBXML = service.MakeQBXMLDocument();
                            var jobDocument = jobQBXML.Item1;
                            var jobElement = jobQBXML.Item2;

                            // Build the request to create the job.
                            service.BuildCustomerAddRqForJob(jobDocument, jobElement, new QuickBooksCustomer()
                            {
                                Name = name
                            }, parentName);

                            // Make the request.
                            var jobResponse = req.ProcessRequest(ticket, jobDocument.OuterXml);
                        }
                    }

                    // Customer Only
                    else
                    {
                        // Prepare a new QBXML document.
                        var custQBXML = service.MakeQBXMLDocument();
                        var custDocument = custQBXML.Item1;
                        var custElement = custQBXML.Item2;

                        // Build the request to create the customer.
                        service.BuildCustomerAddRqForCustomer(custDocument, custElement, name);

                        // Make the request.
                        var custResponse = req.ProcessRequest(ticket, custDocument.OuterXml);
                    }
                }

                StatusText += string.Format("{0} - Finishing Up.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

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

        private List<string> GetProjects()
        {
            StatusText += string.Format("{0} - Getting projects from server.\r\n", DateTime.Now.ToString());
            OnPropertyChanged("StatusText");

            // Build request.
            var request = new RestRequest("odata/Jobs?$select=QuickBooksCustomerJob&$filter=(QuickBooksCustomerJob ne null)", Method.GET);

            // Execute request.
            var response = client.Execute<ODataResponse<Job>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                StatusText += string.Format("{0} - Got projects from server.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                return response.Data.Value.Select(j => j.QuickBooksCustomerJob).ToList();
            }
            else
            {
                // Enable the buttons
                IsExitEnabled = true;
                IsTryEnabled = true;
                IsStartOverEnabled = true;
                OnPropertyChanged("IsExitEnabled");
                OnPropertyChanged("IsTryEnabled");
                OnPropertyChanged("IsStartOverEnabled");
                throw new Exception(response.Content);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
