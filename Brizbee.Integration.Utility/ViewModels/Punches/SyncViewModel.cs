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
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Brizbee.Integration.Utility.ViewModels.Punches
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
        private Commit commit;
        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        private List<Punch> punches;
        #endregion

        public void Export()
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
                // Collect the punches to be synced.
                // ------------------------------------------------------------

                var ids = new List<string>();
                punches = GetPunches();

                StatusText += string.Format("{0} - Preparing sync of {1} punches.\r\n", DateTime.Now.ToString(), punches.Count);
                OnPropertyChanged("StatusText");

                // ------------------------------------------------------------
                // Validate the metadata.
                // ------------------------------------------------------------

                // Ensure that all the employees, customers and jobs, service
                // and payroll items exist in the Company File and notify the user.
                ValidateEmployees(req, ticket);
                ValidateServiceItems(req, ticket);
                ValidatePayrollItems(req, ticket);
                ValidateCustomers(req, ticket);
                ValidateClasses(req, ticket);

                // Do not continue if there are validation errors.
                if (ValidationErrorCount > 0)
                {
                    StatusText += string.Format("{0} - Sync failed. Please correct the {1} validation errors first.\r\n", DateTime.Now.ToString(), ValidationErrorCount);
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
                    // ------------------------------------------------------------
                    // Record the punches.
                    // ------------------------------------------------------------

                    // Prepare a new QBXML document.
                    var punchQBXML = service.MakeQBXMLDocument();
                    var punchDocument = punchQBXML.Item1;
                    var punchElement = punchQBXML.Item2;

                    // Add an element for each punch to be synced
                    foreach (var punch in punches)
                    {
                        BuildTimeTrackingAddRq(punchDocument, punchElement, punch);
                    }

                    Trace.TraceInformation(punchDocument.OuterXml);

                    // Make the request.
                    var punchResponse = req.ProcessRequest(ticket, punchDocument.OuterXml);

                    Trace.TraceInformation(punchResponse);

                    // Then walk the response.
                    var walkReponse = WalkTimeTrackingAddRs(punchResponse);
                    ids = walkReponse.Item3;

                    if (SaveErrorCount > 0)
                    {
                        StatusText += string.Format("{0} - Sync failed. Please correct the {1} errors first.\r\n", DateTime.Now.ToString(), SaveErrorCount);
                        OnPropertyChanged("StatusText");

                        // Enable the buttons.
                        IsExitEnabled = true;
                        IsTryEnabled = true;
                        IsStartOverEnabled = true;
                        OnPropertyChanged("IsExitEnabled");
                        OnPropertyChanged("IsTryEnabled");
                        OnPropertyChanged("IsStartOverEnabled");
                    }
                    else
                    {
                        StatusText += string.Format("{0} - Finishing Up.\r\n", DateTime.Now.ToString());
                        OnPropertyChanged("StatusText");

                        StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                        OnPropertyChanged("StatusText");

                        // Enable the buttons.
                        IsExitEnabled = true;
                        IsStartOverEnabled = true;
                        OnPropertyChanged("IsExitEnabled");
                        OnPropertyChanged("IsStartOverEnabled");
                    }
                }

                // ------------------------------------------------------------
                // Save the sync.
                // ------------------------------------------------------------

                var export = new QuickBooksDesktopExport()
                {
                    QBProductName = hostDetails.QBProductName,
                    QBCountry = hostDetails.QBCountry,
                    QBMajorVersion = hostDetails.QBMajorVersion,
                    QBMinorVersion = hostDetails.QBMinorVersion,
                    QBSupportedQBXMLVersions = hostDetails.QBSupportedQBXMLVersions,
                    CreatedAt = DateTime.UtcNow,
                    UserId = int.Parse(Application.Current.Properties["AuthUserId"] as string),
                    CommitId = commit.Id,
                    InAt = commit.InAt,
                    OutAt = commit.OutAt,
                    Log = StatusText,
                    TxnIDs = string.Join(",", ids)
                };

                // Build the request.
                var syncHttpRequest = new RestRequest("odata/QuickBooksDesktopExports", Method.POST);
                syncHttpRequest.AddJsonBody(export);

                // Execute request.
                var syncHttpResponse = client.Execute(syncHttpRequest);
                if ((syncHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (syncHttpResponse.StatusCode == System.Net.HttpStatusCode.Created))
                {
                    StatusText += string.Format("{0} - Synced Successfully.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    Trace.TraceWarning(syncHttpResponse.Content);
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

        private List<Punch> GetPunches()
        {
            commit = Application.Current.Properties["SelectedCommit"] as Commit;

            StatusText += string.Format("{0} - Getting punches for {1} thru {2}.\r\n", DateTime.Now.ToString(), commit.InAt.ToString("yyyy-MM-dd"), commit.OutAt.ToString("yyyy-MM-dd"));
            OnPropertyChanged("StatusText");

            // Build request to retrieve punches
            var request = new RestRequest(string.Format("odata/Punches/Default.Download(CommitId={0})", commit.Id), Method.GET);

            // Execute request to retrieve punches
            var response = client.Execute<List<Punch>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                StatusText += string.Format("{0} - Got punches from server.\r\n", DateTime.Now.ToString());
                OnPropertyChanged("StatusText");

                return response.Data;
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

        private async System.Threading.Tasks.Task<Commit> UpdateLock(Commit commit, DateTime exportedAt)
        {
            // Build the request
            var url = string.Format("odata/Commits({0})", commit.Id);
            var request = new RestRequest(url, Method.PATCH);
            request.AddJsonBody(new
            {
                QuickBooksExportedAt = exportedAt
            });

            // Execute request
            var response = await client.ExecuteAsync<Commit>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.NoContent))
            {
                return response.Data;
            }
            else
            {
                throw new Exception(response.Content);
            }
        }

        private XmlElement MakeSimpleElem(XmlDocument doc, string tagName, string tagVal)
        {
            XmlElement elem = doc.CreateElement(tagName);
            elem.InnerText = tagVal;
            return elem;
        }

        private string ReplaceCharacters(string value = "")
        {
            var replaced = string.IsNullOrEmpty(value) ? "" : value;

            // < less than character should be changed to &lt;
            replaced = replaced.Replace("<", "&lt;");

            // > greater than character should be changed to &gt;
            replaced = replaced.Replace(">", "&gt;");

            // ' single quote character should be changed to &apos;
            replaced = replaced.Replace("'", "&apos;");

            // " double quote character should be changed to &quot;
            replaced = replaced.Replace("\"", "&quot;");

            // & ampersand character should be changed to &amp;
            replaced = replaced.Replace("&", "&amp;");

            return replaced;
        }

        private void BuildTimeTrackingAddRq(XmlDocument doc, XmlElement parent, Punch punch)
        {
            var date = punch.InAt.ToString("yyyy-MM-dd");
            var customerName = ReplaceCharacters(punch.Task.Job.QuickBooksCustomerJob);
            var className = ReplaceCharacters(punch.Task.Job.QuickBooksClass);
            var employeeName = ReplaceCharacters(punch.User.QuickBooksEmployee);
            var span = punch.OutAt.Value.Subtract(punch.InAt);
            var duration = string.Format("PT{0}H{1}M", span.Hours, span.Minutes);
            var guid = string.Format("{{{0}}}", punch.Guid.ToString());
            var payrollItem = punch.PayrollRate.QBDPayrollItem;
            var serviceItem = punch.ServiceRate.QBDServiceItem;

            // Create TimeTrackingAddRq aggregate and fill in field values for it
            XmlElement TimeTrackingAddRq = doc.CreateElement("TimeTrackingAddRq");
            parent.AppendChild(TimeTrackingAddRq);

            // Create TimeTrackingAdd aggregate and fill in field values for it
            XmlElement TimeTrackingAdd = doc.CreateElement("TimeTrackingAdd");
            TimeTrackingAddRq.AppendChild(TimeTrackingAdd);
            // Set field value for TxnDate
            TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "TxnDate", date));

            // Create EntityRef aggregate and fill in field values for it
            XmlElement EntityRef = doc.CreateElement("EntityRef");
            TimeTrackingAdd.AppendChild(EntityRef);
            // Set field value for FullName
            EntityRef.AppendChild(MakeSimpleElem(doc, "FullName", employeeName));

            if (!string.IsNullOrEmpty(customerName))
            {
                // Create CustomerRef aggregate and fill in field values for it
                XmlElement CustomerRef = doc.CreateElement("CustomerRef");
                TimeTrackingAdd.AppendChild(CustomerRef);
                // Set field value for FullName
                CustomerRef.AppendChild(MakeSimpleElem(doc, "FullName", customerName));
            }

            if (!string.IsNullOrEmpty(serviceItem))
            {
                // Create ItemServiceRef aggregate and fill in field values for it
                XmlElement ItemServiceRef = doc.CreateElement("ItemServiceRef");
                TimeTrackingAdd.AppendChild(ItemServiceRef);
                // Set field value for FullName
                ItemServiceRef.AppendChild(MakeSimpleElem(doc, "FullName", serviceItem));
            }

            // Set field value for Duration
            TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "Duration", duration));

            // Create ClassRef aggregate and fill in field values for it
            if (!string.IsNullOrEmpty(className))
            {
                XmlElement ClassRef = doc.CreateElement("ClassRef");
                TimeTrackingAdd.AppendChild(ClassRef);
                // Set field value for FullName
                ClassRef.AppendChild(MakeSimpleElem(doc, "FullName", className));
            }

            if (!string.IsNullOrEmpty(payrollItem))
            {
                // Create PayrollItemWageRef aggregate and fill in field values for it
                XmlElement PayrollItemWageRef = doc.CreateElement("PayrollItemWageRef");
                TimeTrackingAdd.AppendChild(PayrollItemWageRef);
                // Set field value for FullName
                PayrollItemWageRef.AppendChild(MakeSimpleElem(doc, "FullName", payrollItem));
            }

            //// Set field value for Notes <!-- optional -->
            //TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "Notes", "ab"));
            //// Set field value for BillableStatus <!-- optional -->
            //TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "BillableStatus", "Billable"));
            //// Set field value for IsBillable <!-- optional -->
            //TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "IsBillable", "1"));
            // Set field value for ExternalGUID <!-- optional -->
            TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "ExternalGUID", guid));

            // Set field value for IncludeRetElement <!-- optional, may repeat -->
            //TimeTrackingAddRq.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "ab"));
        }

        private void BuildEmployeeQueryRq(XmlDocument doc, XmlElement parent, string employeeFullName)
        {
            // Create EmployeeQueryRq aggregate and fill in field values for it
            XmlElement EmployeeQueryRq = doc.CreateElement("EmployeeQueryRq");
            parent.AppendChild(EmployeeQueryRq);

            // Populate the FullName field
            EmployeeQueryRq.AppendChild(MakeSimpleElem(doc, "FullName", employeeFullName));

            // Only include certain fields
            EmployeeQueryRq.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildItemServiceQueryRq(XmlDocument doc, XmlElement parent, string serviceItemName)
        {
            // Create ItemServiceQueryRq aggregate and fill in field values for it
            XmlElement ItemServiceQuery = doc.CreateElement("ItemServiceQueryRq");
            parent.AppendChild(ItemServiceQuery);

            // Populate the FullName field
            ItemServiceQuery.AppendChild(MakeSimpleElem(doc, "FullName", serviceItemName));

            // Only include certain fields
            ItemServiceQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildPayrollItemWageQueryRq(XmlDocument doc, XmlElement parent, string serviceItemName)
        {
            // Create PayrollItemWageQueryRq aggregate and fill in field values for it
            XmlElement PayrollItemWageQuery = doc.CreateElement("PayrollItemWageQueryRq");
            parent.AppendChild(PayrollItemWageQuery);

            // Populate the FullName field
            PayrollItemWageQuery.AppendChild(MakeSimpleElem(doc, "FullName", serviceItemName));

            // Only include certain fields
            PayrollItemWageQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildCustomerQueryRq(XmlDocument doc, XmlElement parent, string customerName)
        {
            // Create CustomerQueryRq aggregate and fill in field values for it
            XmlElement CustomerQuery = doc.CreateElement("CustomerQueryRq");
            parent.AppendChild(CustomerQuery);

            // Populate the FullName field
            CustomerQuery.AppendChild(MakeSimpleElem(doc, "FullName", customerName));

            // Only include certain fields
            CustomerQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildClassQueryRq(XmlDocument doc, XmlElement parent, string className)
        {
            // Create ClassQueryRq aggregate and fill in field values for it
            XmlElement ClassQueryRq = doc.CreateElement("ClassQueryRq");
            parent.AppendChild(ClassQueryRq);

            // Populate the FullName field
            ClassQueryRq.AppendChild(MakeSimpleElem(doc, "FullName", className));

            // Only include certain fields
            ClassQueryRq.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private (bool, string, List<string>) WalkTimeTrackingAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("TimeTrackingAddRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "TxnID":
                                id = xmlNode.InnerText;
                                break;
                        }
                    }

                    ids.Add(id);
                }

                return (true, "", ids);
            }
            else if (iStatusCode == 3310)
            {
                Regex regex = new Regex(@".+(employee ""){1}(.+)("" provided){1}.+");
                Match match = regex.Match(statusMessage);

                if (match.Success)
                {
                    var employee = match.Groups[2].Value;
                    var errorMessage = string.Format("Cannot determine if the employee in the company file should use time data to create paychecks. Please configure this for the employee: {0}", employee);
                    SaveErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                    OnPropertyChanged("StatusText");
                }

                return (false, statusMessage, null);
            }
            else if (iStatusCode == 3231)
            {
                // Do nothing, it's just telling us that the request wasn't processed
                return (false, statusMessage, null);
            }
            else if (iStatusCode > 0)
            {
                SaveErrorCount += 1;
                StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                OnPropertyChanged("StatusText");

                return (false, statusMessage, null);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        private void WalkEmployeeQueryRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList EmployeeQueryRsList = responseXmlDoc.GetElementsByTagName("EmployeeQueryRs");
            foreach (var result in EmployeeQueryRsList)
            {
                XmlNode responseNode = result as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                //status code = 0 all OK, > 0 is warning
                if (iStatusCode == 500)
                {
                    Regex regex = new Regex(@".+(\(""){1}(.+)(""\)){1}.+");
                    Match match = regex.Match(statusMessage);

                    if (match.Success)
                    {
                        var employeeName = match.Groups[2].Value;
                        var errorMessage = string.Format("Missing employee in the company file: {0}", employeeName);
                        ValidationErrorCount += 1;
                        StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                        OnPropertyChanged("StatusText");
                    }
                }
                else if (iStatusCode > 0)
                {
                    ValidationErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                    OnPropertyChanged("StatusText");
                }
            }
        }

        private void WalkItemServiceQueryRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList ItemServiceQueryRsList = responseXmlDoc.GetElementsByTagName("ItemServiceQueryRs");
            foreach (var result in ItemServiceQueryRsList)
            {
                XmlNode responseNode = result as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                //status code = 0 all OK, > 0 is warning
                if (iStatusCode == 500)
                {
                    Regex regex = new Regex(@".+(\(""){1}(.+)(""\)){1}.+");
                    Match match = regex.Match(statusMessage);

                    if (match.Success)
                    {
                        var serviceItem = match.Groups[2].Value;
                        var errorMessage = string.Format("Missing service item in the company file: {0}", serviceItem);
                        ValidationErrorCount += 1;
                        StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                        OnPropertyChanged("StatusText");
                    }
                }
                else if (iStatusCode > 0)
                {
                    ValidationErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                    OnPropertyChanged("StatusText");
                }
            }
        }

        private void WalkPayrollItemWageQueryRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList PayrollItemWageQueryRsList = responseXmlDoc.GetElementsByTagName("PayrollItemWageQueryRs");
            foreach (var result in PayrollItemWageQueryRsList)
            {
                XmlNode responseNode = result as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                //status code = 0 all OK, > 0 is warning
                if (iStatusCode == 500)
                {
                    Regex regex = new Regex(@".+(\(""){1}(.+)(""\)){1}.+");
                    Match match = regex.Match(statusMessage);

                    if (match.Success)
                    {
                        var payrollItem = match.Groups[2].Value;
                        var errorMessage = string.Format("Missing payroll item in the company file: {0}", payrollItem);
                        ValidationErrorCount += 1;
                        StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                        OnPropertyChanged("StatusText");
                    }
                }
                else if (iStatusCode > 0)
                {
                    ValidationErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                    OnPropertyChanged("StatusText");
                }
            }
        }

        private void WalkCustomerQueryRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList CustomerQueryRsList = responseXmlDoc.GetElementsByTagName("CustomerQueryRs");
            foreach (var result in CustomerQueryRsList)
            {
                XmlNode responseNode = result as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                //status code = 0 all OK, > 0 is warning
                if (iStatusCode == 500)
                {
                    Regex regex = new Regex(@".+(\(""){1}(.+)(""\)){1}.+");
                    Match match = regex.Match(statusMessage);

                    if (match.Success)
                    {
                        var customerName = match.Groups[2].Value;
                        var errorMessage = string.Format("Missing customer:job in the company file: {0}", customerName);
                        ValidationErrorCount += 1;
                        StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                        OnPropertyChanged("StatusText");
                    }
                }
                else if (iStatusCode > 0)
                {
                    ValidationErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                    OnPropertyChanged("StatusText");
                }
            }
        }

        private void WalkClassQueryRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList ClassQueryRsList = responseXmlDoc.GetElementsByTagName("ClassQueryRs");
            foreach (var result in ClassQueryRsList)
            {
                XmlNode responseNode = result as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                //status code = 0 all OK, > 0 is warning
                if (iStatusCode == 500)
                {
                    Regex regex = new Regex(@".+(\(""){1}(.+)(""\)){1}.+");
                    Match match = regex.Match(statusMessage);

                    if (match.Success)
                    {
                        var className = match.Groups[2].Value;
                        var errorMessage = string.Format("Missing class in the company file: {0}", className);
                        ValidationErrorCount += 1;
                        StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), errorMessage);
                        OnPropertyChanged("StatusText");
                    }
                }
                else if (iStatusCode > 0)
                {
                    ValidationErrorCount += 1;
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), statusMessage);
                    OnPropertyChanged("StatusText");
                }
            }
        }

        private void WalkTimeTrackingRet(XmlNode TimeTrackingRet)
        {
            if (TimeTrackingRet == null) return;

            //Go through all the elements of TimeTrackingRet
            //Get value of TxnID
            string TxnID = TimeTrackingRet.SelectSingleNode("./TxnID").InnerText;
            //Get value of TimeCreated
            string TimeCreated = TimeTrackingRet.SelectSingleNode("./TimeCreated").InnerText;
            //Get value of TimeModified
            string TimeModified = TimeTrackingRet.SelectSingleNode("./TimeModified").InnerText;
            //Get value of EditSequence
            string EditSequence = TimeTrackingRet.SelectSingleNode("./EditSequence").InnerText;
            //Get value of TxnNumber
            if (TimeTrackingRet.SelectSingleNode("./TxnNumber") != null)
            {
                string TxnNumber = TimeTrackingRet.SelectSingleNode("./TxnNumber").InnerText;
            }
            //Get value of TxnDate
            string TxnDate = TimeTrackingRet.SelectSingleNode("./TxnDate").InnerText;
            //Get all field values for EntityRef aggregate
            //Get value of ListID
            if (TimeTrackingRet.SelectSingleNode("./EntityRef/ListID") != null)
            {
                string ListID = TimeTrackingRet.SelectSingleNode("./EntityRef/ListID").InnerText;
            }
            //Get value of FullName
            if (TimeTrackingRet.SelectSingleNode("./EntityRef/FullName") != null)
            {
                string FullName = TimeTrackingRet.SelectSingleNode("./EntityRef/FullName").InnerText;
            }
            //Done with field values for EntityRef aggregate

            //Get all field values for CustomerRef aggregate
            XmlNode CustomerRef = TimeTrackingRet.SelectSingleNode("./CustomerRef");
            if (CustomerRef != null)
            {
                //Get value of ListID
                if (TimeTrackingRet.SelectSingleNode("./CustomerRef/ListID") != null)
                {
                    string ListID = TimeTrackingRet.SelectSingleNode("./CustomerRef/ListID").InnerText;
                }
                //Get value of FullName
                if (TimeTrackingRet.SelectSingleNode("./CustomerRef/FullName") != null)
                {
                    string FullName = TimeTrackingRet.SelectSingleNode("./CustomerRef/FullName").InnerText;
                }
            }
            //Done with field values for CustomerRef aggregate

            //Get all field values for ItemServiceRef aggregate
            XmlNode ItemServiceRef = TimeTrackingRet.SelectSingleNode("./ItemServiceRef");
            if (ItemServiceRef != null)
            {
                //Get value of ListID
                if (TimeTrackingRet.SelectSingleNode("./ItemServiceRef/ListID") != null)
                {
                    string ListID = TimeTrackingRet.SelectSingleNode("./ItemServiceRef/ListID").InnerText;
                }
                //Get value of FullName
                if (TimeTrackingRet.SelectSingleNode("./ItemServiceRef/FullName") != null)
                {
                    string FullName = TimeTrackingRet.SelectSingleNode("./ItemServiceRef/FullName").InnerText;
                }
            }
            //Done with field values for ItemServiceRef aggregate

            //Get value of Duration
            string Duration = TimeTrackingRet.SelectSingleNode("./Duration").InnerText;
            //Get all field values for ClassRef aggregate
            XmlNode ClassRef = TimeTrackingRet.SelectSingleNode("./ClassRef");
            if (ClassRef != null)
            {
                //Get value of ListID
                if (TimeTrackingRet.SelectSingleNode("./ClassRef/ListID") != null)
                {
                    string ListID = TimeTrackingRet.SelectSingleNode("./ClassRef/ListID").InnerText;
                }
                //Get value of FullName
                if (TimeTrackingRet.SelectSingleNode("./ClassRef/FullName") != null)
                {
                    string FullName = TimeTrackingRet.SelectSingleNode("./ClassRef/FullName").InnerText;
                }
            }
            //Done with field values for ClassRef aggregate

            //Get all field values for PayrollItemWageRef aggregate
            XmlNode PayrollItemWageRef = TimeTrackingRet.SelectSingleNode("./PayrollItemWageRef");
            if (PayrollItemWageRef != null)
            {
                //Get value of ListID
                if (TimeTrackingRet.SelectSingleNode("./PayrollItemWageRef/ListID") != null)
                {
                    string ListID = TimeTrackingRet.SelectSingleNode("./PayrollItemWageRef/ListID").InnerText;
                }
                //Get value of FullName
                if (TimeTrackingRet.SelectSingleNode("./PayrollItemWageRef/FullName") != null)
                {
                    string FullName = TimeTrackingRet.SelectSingleNode("./PayrollItemWageRef/FullName").InnerText;
                }
            }
            //Done with field values for PayrollItemWageRef aggregate

            //Get value of Notes
            if (TimeTrackingRet.SelectSingleNode("./Notes") != null)
            {
                string Notes = TimeTrackingRet.SelectSingleNode("./Notes").InnerText;
            }
            //Get value of BillableStatus
            if (TimeTrackingRet.SelectSingleNode("./BillableStatus") != null)
            {
                string BillableStatus = TimeTrackingRet.SelectSingleNode("./BillableStatus").InnerText;
            }
            //Get value of ExternalGUID
            if (TimeTrackingRet.SelectSingleNode("./ExternalGUID") != null)
            {
                string ExternalGUID = TimeTrackingRet.SelectSingleNode("./ExternalGUID").InnerText;
            }
            //Get value of IsBillable
            if (TimeTrackingRet.SelectSingleNode("./IsBillable") != null)
            {
                string IsBillable = TimeTrackingRet.SelectSingleNode("./IsBillable").InnerText;
            }
            //Get value of IsBilled
            if (TimeTrackingRet.SelectSingleNode("./IsBilled") != null)
            {
                string IsBilled = TimeTrackingRet.SelectSingleNode("./IsBilled").InnerText;
            }
        }

        /// <summary>
        /// Uses the given QuickBooks connection details to verify that the employees
        /// in the list of punches exists.
        /// </summary>
        /// <param name="req">QuickBooks request processor</param>
        /// <param name="ticket">Security token</param>
        private void ValidateEmployees(RequestProcessor2 req, string ticket)
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

            var employees = punches.GroupBy(p => p.User.QuickBooksEmployee).Select(g => g.Key);
            foreach (var name in employees)
            {
                BuildEmployeeQueryRq(doc, inner, name);
            }

            var response = req.ProcessRequest(ticket, doc.OuterXml);

            WalkEmployeeQueryRs(response);
        }

        /// <summary>
        /// Uses the given QuickBooks connection details to verify that the service items
        /// in the list of punches exists.
        /// </summary>
        /// <param name="req">QuickBooks request processor</param>
        /// <param name="ticket">Security token</param>
        private void ValidateServiceItems(RequestProcessor2 req, string ticket)
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

            var serviceItems = punches.GroupBy(p => p.Task.QuickBooksServiceItem).Select(g => g.Key);
            foreach (var name in serviceItems)
            {
                BuildItemServiceQueryRq(doc, inner, name);
            }

            var response = req.ProcessRequest(ticket, doc.OuterXml);

            WalkItemServiceQueryRs(response);
        }

        /// <summary>
        /// Uses the given QuickBooks connection details to verify that the payroll items
        /// in the list of punches exists.
        /// </summary>
        /// <param name="req">QuickBooks request processor</param>
        /// <param name="ticket">Security token</param>
        private void ValidatePayrollItems(RequestProcessor2 req, string ticket)
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

            var payrollItems = punches.GroupBy(p => p.Task.QuickBooksPayrollItem).Select(g => g.Key);
            foreach (var name in payrollItems)
            {
                BuildPayrollItemWageQueryRq(doc, inner, name);
            }

            var response = req.ProcessRequest(ticket, doc.OuterXml);

            WalkPayrollItemWageQueryRs(response);
        }

        /// <summary>
        /// Uses the given QuickBooks connection details to verify that the customers
        /// in the list of punches exists.
        /// </summary>
        /// <param name="req">QuickBooks request processor</param>
        /// <param name="ticket">Security token</param>
        private void ValidateCustomers(RequestProcessor2 req, string ticket)
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

            var customers = punches.GroupBy(p => p.Task.Job.QuickBooksCustomerJob).Select(g => g.Key);

            // Do not continue if there are no customers.
            if (!customers.Any())
                return;

            foreach (var name in customers)
            {
                BuildCustomerQueryRq(doc, inner, name);
            }

            var response = req.ProcessRequest(ticket, doc.OuterXml);

            WalkCustomerQueryRs(response);
        }

        /// <summary>
        /// Uses the given QuickBooks connection details to verify that the classes
        /// in the list of punches exists.
        /// </summary>
        /// <param name="req">QuickBooks request processor</param>
        /// <param name="ticket">Security token</param>
        private void ValidateClasses(RequestProcessor2 req, string ticket)
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

            var classes = punches.GroupBy(p => p.Task.Job.QuickBooksClass).Select(g => g.Key);

            // Do not continue if there are no classes.
            if (!classes.Any())
                return;

            foreach (var name in classes)
            {
                BuildClassQueryRq(doc, inner, name);
            }

            var response = req.ProcessRequest(ticket, doc.OuterXml);

            WalkClassQueryRs(response);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
