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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;

namespace Brizbee.Integration.Utility.ViewModels.Punches
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

        private Commit _commit;

        private readonly RestClient _client = Application.Current.Properties["Client"] as RestClient;

        private List<Punch> _punches;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                _punches = GetPunches();

                StatusText += string.Format("{0} - Preparing sync of {1} punches.\r\n", DateTime.Now.ToString(), _punches.Count);
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

                    // Add an element for each punch to be synced
                    foreach (var punch in _punches)
                    {
                        // Prepare a new QBXML document.
                        var punchQbxml = service.MakeQBXMLDocument();
                        var punchDocument = punchQbxml.Item1;
                        var punchElement = punchQbxml.Item2;

                        BuildTimeTrackingAddRq(punchDocument, punchElement, punch);

                        // Make the request.
                        Logger.Debug(punchDocument.OuterXml);
                        var punchResponse = req.ProcessRequest(ticket, punchDocument.OuterXml);
                        Logger.Debug(punchResponse);

                        // Then walk the response.
                        var walkResponse = WalkTimeTrackingAddRs(punchResponse);
                        ids = ids.Concat(walkResponse.Item3).ToList();
                    }

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
                        StatusText += string.Format("{0} - Finishing up.\r\n", DateTime.Now.ToString());
                        OnPropertyChanged("StatusText");
                    }
                }


                // ------------------------------------------------------------
                // Save the sync details.
                // ------------------------------------------------------------

                // Build the payload.
                var payload = new
                {
                    QBProductName = hostDetails.QBProductName,
                    QBCountry = hostDetails.QBCountry,
                    QBMajorVersion = hostDetails.QBMajorVersion,
                    QBMinorVersion = hostDetails.QBMinorVersion,
                    QBSupportedQBXMLVersions = hostDetails.QBSupportedQBXMLVersions,
                    CreatedAt = new DateTimeOffset(DateTime.UtcNow),
                    CommitId = _commit.Id,
                    InAt = new DateTimeOffset(_commit.InAt),
                    OutAt = new DateTimeOffset(_commit.OutAt),
                    Log = StatusText,
                    TxnIDs = string.Join(",", ids)
                };

                // Build the request to send the export details.
                var syncHttpRequest = new RestRequest("odata/QuickBooksDesktopExports", Method.Post);
                syncHttpRequest.AddJsonBody(payload);

                // Execute request.
                var syncHttpResponse = _client.Execute(syncHttpRequest);
                if ((syncHttpResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (syncHttpResponse.StatusCode == System.Net.HttpStatusCode.OK))
                {
                    StatusText += string.Format("{0} - Synced successfully.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }
                else
                {
                    StatusText += string.Format("{0} - {1}\r\n", DateTime.Now.ToString(), syncHttpResponse.Content);
                    OnPropertyChanged("StatusText");

                    StatusText += string.Format("{0} - Could not save sync details, reversing.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");


                    // ------------------------------------------------------------
                    // Reverse the transaction.
                    // ------------------------------------------------------------

                    foreach (var txnId in ids)
                    {
                        // Prepare a new QBXML document.
                        var delQBXML = service.MakeQBXMLDocument();
                        var delDocument = delQBXML.Item1;
                        var delElement = delQBXML.Item2;
                        service.BuildTxnDelRq(delDocument, delElement, "TimeTracking", txnId);

                        // Make the request.
                        Logger.Debug(delDocument.OuterXml);
                        var delResponse = req.ProcessRequest(ticket, delDocument.OuterXml);
                        Logger.Debug(delResponse);

                        // Then walk the response.
                        var delWalkResponse = service.WalkTxnDelRs(delResponse);
                    }

                    StatusText += string.Format("{0} - Sync failed.\r\n", DateTime.Now.ToString());
                    OnPropertyChanged("StatusText");
                }

                // Close the QuickBooks connection.
                req.EndSession(ticket);
                req.CloseConnection();
                req = null;
            }
            catch (DownloadFailedException)
            {
                StatusText += string.Format("{0} - Sync failed. Could not download punches from the server.\r\n", DateTime.Now.ToString());
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

        private List<Punch> GetPunches()
        {
            _commit = Application.Current.Properties["SelectedCommit"] as Commit;

            StatusText += string.Format("{0} - Getting punches for {1} thru {2}.\r\n", DateTime.Now.ToString(), _commit.InAt.ToString("yyyy-MM-dd"), _commit.OutAt.ToString("yyyy-MM-dd"));
            OnPropertyChanged("StatusText");

            // Build request to retrieve punches
            var request = new RestRequest($"odata/Punches/Default.Download(CommitId={_commit.Id})");

            // Execute request to retrieve punches
            var response = _client.Execute<List<Punch>>(request);
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

        private static XmlElement MakeSimpleElem(XmlDocument doc, string tagName, string tagVal)
        {
            var element = doc.CreateElement(tagName);
            element.InnerText = tagVal;
            return element;
        }

        private string ReplaceCharacters(string value = "")
        {
            var replaced = string.IsNullOrEmpty(value) ? "" : value;

            // & ampersand character should be changed to &amp;
            replaced = replaced.Replace("&", "&amp;");

            // < less than character should be changed to &lt;
            replaced = replaced.Replace("<", "&lt;");

            // > greater than character should be changed to &gt;
            replaced = replaced.Replace(">", "&gt;");

            // ' single quote character should be changed to &apos;
            replaced = replaced.Replace("'", "&apos;");

            // " double quote character should be changed to &quot;
            replaced = replaced.Replace("\"", "&quot;");

            // # pound character should be changed to &#35;
            replaced = replaced.Replace("#", "&#35;");

            // + plus character should be changed to &#43;
            replaced = replaced.Replace("+", "&#43;");

            // / slash character should be changed to &#47;
            replaced = replaced.Replace("/", "&#47;");

            // NOT escaping the following charaters:
            // ( ) * , - .

            //// ( left parenthesis character should be changed to &#40;
            //replaced = replaced.Replace("(", "&#40;");

            //// ) right parenthesis character should be changed to &#41;
            //replaced = replaced.Replace(")", "&#41;");

            //// * asterisk character should be changed to &#42;
            //replaced = replaced.Replace("*", "&#42;");

            //// , comma character should be changed to &#44;
            //replaced = replaced.Replace(",", "&#44;");

            //// - hyphen character should be changed to &#45;
            //replaced = replaced.Replace("-", "&#45;");

            //// . period character should be changed to &#46;
            //replaced = replaced.Replace(".", "&#46;");

            return replaced;
        }

        private static void BuildTimeTrackingAddRq(XmlDocument doc, XmlElement parent, Punch punch)
        {
            var date = punch.InAt.ToString("yyyy-MM-dd");

            // 08/31 Disabling replace characters because it seems to be handled automatically
            //var customerName = ReplaceCharacters(punch.Task.Job.QuickBooksCustomerJob);
            //var className = ReplaceCharacters(punch.Task.Job.QuickBooksClass);
            //var employeeName = ReplaceCharacters(punch.User.QuickBooksEmployee);

            var customerName = punch.Task.Job.QuickBooksCustomerJob;
            var className = punch.Task.Job.QuickBooksClass;
            var employeeName = punch.User.QuickBooksEmployee;
            var vendorName = punch.User.QuickBooksVendor;
            var span = punch.OutAt.Value.Subtract(punch.InAt);
            var duration = $"PT{span.Hours}H{span.Minutes}M";
            var guid = $"{{{punch.Guid}}}";
            var payrollItem = punch.PayrollRate.QBDPayrollItem;
            var serviceItem = punch.ServiceRate.QBDServiceItem;

            // Build the request to add a time tracking entry.
            var timeTrackingAddRq = doc.CreateElement("TimeTrackingAddRq");
            parent.AppendChild(timeTrackingAddRq);

            // Add date for the time tracking entry.
            var timeTrackingAdd = doc.CreateElement("TimeTrackingAdd");
            timeTrackingAddRq.AppendChild(timeTrackingAdd);
            timeTrackingAdd.AppendChild(MakeSimpleElem(doc, "TxnDate", date));

            // Entity can be either a vendor or employee, depending on what has been specified.
            string entityName;
            if (!string.IsNullOrEmpty(employeeName))
            {
                entityName = employeeName;
            }
            else if (!string.IsNullOrEmpty(vendorName))
            {
                entityName = vendorName;
            }
            else
            {
                return;
            }
            
            // Add the entity.
            var entityRef = doc.CreateElement("EntityRef");
            timeTrackingAdd.AppendChild(entityRef);
            entityRef.AppendChild(MakeSimpleElem(doc, "FullName", entityName));

            // Add customer name if it is available.
            if (!string.IsNullOrEmpty(customerName))
            {
                var customerRef = doc.CreateElement("CustomerRef");
                timeTrackingAdd.AppendChild(customerRef);
                customerRef.AppendChild(MakeSimpleElem(doc, "FullName", customerName));
            }

            // Add service item for employee or vendor.
            if (!string.IsNullOrEmpty(serviceItem))
            {
                var itemServiceRef = doc.CreateElement("ItemServiceRef");
                timeTrackingAdd.AppendChild(itemServiceRef);
                itemServiceRef.AppendChild(MakeSimpleElem(doc, "FullName", serviceItem));
            }

            // Add duration in minutes.
            timeTrackingAdd.AppendChild(MakeSimpleElem(doc, "Duration", duration));
            
            // Add class reference.
            if (!string.IsNullOrEmpty(className))
            {
                var classRef = doc.CreateElement("ClassRef");
                timeTrackingAdd.AppendChild(classRef);
                classRef.AppendChild(MakeSimpleElem(doc, "FullName", className));
            }

            // Add a payroll item if there is one AND no vendor has been specified.
            if (!string.IsNullOrEmpty(payrollItem) && string.IsNullOrEmpty(vendorName))
            {
                var payrollItemWageRef = doc.CreateElement("PayrollItemWageRef");
                timeTrackingAdd.AppendChild(payrollItemWageRef);
                payrollItemWageRef.AppendChild(MakeSimpleElem(doc, "FullName", payrollItem));
            }

            // External GUID allows for deleting the time tracking element later.
            timeTrackingAdd.AppendChild(MakeSimpleElem(doc, "ExternalGUID", guid));
        }

        private void BuildEmployeeQueryRq(XmlDocument doc, XmlElement parent, string employeeFullName)
        {
            // Create EmployeeQueryRq aggregate and fill in field values for it
            var employeeQueryRq = doc.CreateElement("EmployeeQueryRq");
            parent.AppendChild(employeeQueryRq);

            // Populate the FullName field
            employeeQueryRq.AppendChild(MakeSimpleElem(doc, "FullName", employeeFullName));

            // Only include certain fields
            employeeQueryRq.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildItemServiceQueryRq(XmlDocument doc, XmlElement parent, string serviceItemName)
        {
            // Create ItemServiceQueryRq aggregate and fill in field values for it
            var itemServiceQuery = doc.CreateElement("ItemServiceQueryRq");
            parent.AppendChild(itemServiceQuery);

            // Populate the FullName field
            itemServiceQuery.AppendChild(MakeSimpleElem(doc, "FullName", serviceItemName));

            // Only include certain fields
            itemServiceQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildPayrollItemWageQueryRq(XmlDocument doc, XmlElement parent, string serviceItemName)
        {
            // Create PayrollItemWageQueryRq aggregate and fill in field values for it
            var payrollItemWageQuery = doc.CreateElement("PayrollItemWageQueryRq");
            parent.AppendChild(payrollItemWageQuery);

            // Populate the FullName field
            payrollItemWageQuery.AppendChild(MakeSimpleElem(doc, "FullName", serviceItemName));

            // Only include certain fields
            payrollItemWageQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildCustomerQueryRq(XmlDocument doc, XmlElement parent, string customerName)
        {
            // Create CustomerQueryRq aggregate and fill in field values for it
            var customerQuery = doc.CreateElement("CustomerQueryRq");
            parent.AppendChild(customerQuery);

            // Populate the FullName field
            customerQuery.AppendChild(MakeSimpleElem(doc, "FullName", customerName));

            // Only include certain fields
            customerQuery.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private void BuildClassQueryRq(XmlDocument doc, XmlElement parent, string className)
        {
            // Create ClassQueryRq aggregate and fill in field values for it
            var classQueryRq = doc.CreateElement("ClassQueryRq");
            parent.AppendChild(classQueryRq);

            // Populate the FullName field
            classQueryRq.AppendChild(MakeSimpleElem(doc, "FullName", className));

            // Only include certain fields
            classQueryRq.AppendChild(MakeSimpleElem(doc, "IncludeRetElement", "Name"));
        }

        private (bool, string, List<string>) WalkTimeTrackingAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("TimeTrackingAddRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

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

            var employees = _punches.GroupBy(p => p.User.QuickBooksEmployee).Select(g => g.Key);
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

            var serviceItems = _punches.GroupBy(p => p.Task.QuickBooksServiceItem).Select(g => g.Key);
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

            var payrollItems = _punches.GroupBy(p => p.Task.QuickBooksPayrollItem).Select(g => g.Key);
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

            var customers = _punches.GroupBy(p => p.Task.Job.QuickBooksCustomerJob).Select(g => g.Key);

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

            var outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            var inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            var classes = _punches.GroupBy(p => p.Task.Job.QuickBooksClass).Select(g => g.Key);

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
