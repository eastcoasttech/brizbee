using Brizbee.Common.Models;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Brizbee.QuickBooksConnector.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public DateTime from = DateTime.Now;
        public DateTime to = DateTime.Now;
        public event PropertyChangedEventHandler PropertyChanged;

        public string AccountButtonTitle { get; set; }
        public string AccountButtonStatus { get; set; }
        public ObservableCollection<Commit> Commits { get; set; }
        public string ExportButtonTitle { get; set; }
        public string ExportButtonStatus { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSignedIn { get; set; }
        public ObservableCollection<Punch> Punches { get; set; }
        public Commit SelectedCommit { get; set; }

        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        public async System.Threading.Tasks.Task Export()
        {
            ExportButtonStatus = "Starting";
            OnPropertyChanged("ExportButtonStatus");

            // Details of this export will be recorded with the commit
            var now = DateTime.Now;

            // Get the punches for this commit from the API
            var punches = await GetPunches();

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

            ExportButtonStatus = "Preparing Export";
            OnPropertyChanged("ExportButtonStatus");

            // Add an element for each punch to be exported
            foreach (var punch in punches)
            {
                BuildTimeTrackingAddRq(doc, inner, punch);
            }

            ExportButtonStatus = "Connecting to QuickBooks";
            OnPropertyChanged("ExportButtonStatus");

            var req = new RequestProcessor2();
            req.OpenConnection2("", "BRIZBEE Connector for QuickBooks", QBXMLRPConnectionType.localQBD);
            var ticket = req.BeginSession("", QBFileMode.qbFileOpenDoNotCare);

            ExportButtonStatus = "Exporting";
            OnPropertyChanged("ExportButtonStatus");

            EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(), doc.OuterXml, EventLogEntryType.Information);

            var response = req.ProcessRequest(ticket, doc.OuterXml);
            req.EndSession(ticket);
            req.CloseConnection();
            req = null;

            WalkTimeTrackingAddRs(response);

            ExportButtonStatus = "Finishing Up";
            OnPropertyChanged("ExportButtonStatus");

            await UpdateCommit(SelectedCommit, now);

            ExportButtonStatus = "Exported Successfully!";
            OnPropertyChanged("ExportButtonStatus");
        }

        public async System.Threading.Tasks.Task RefreshCommits()
        {
            AccountButtonStatus = "Getting Commits from BRIZBEE";
            OnPropertyChanged("AccountButtonStatus");

            // Build request to retrieve commits
            var request = new RestRequest("odata/Commits?$orderby=InAt", Method.GET);

            // Execute request to retrieve authenticated user
            var response = await client.ExecuteTaskAsync<ODataResponse<Commit>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Commits = new ObservableCollection<Commit>(response.Data.Value);
                IsEnabled = true;
                SelectedCommit = Commits[0];
                AccountButtonStatus = "Signed In";
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("SelectedCommit");
                OnPropertyChanged("AccountButtonStatus");
            }
            else
            {
                Commits = new ObservableCollection<Commit>();
                IsEnabled = true;
                AccountButtonStatus = response.ErrorMessage;
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("AccountButtonStatus");
            }
        }

        private void BuildTimeTrackingAddRq(XmlDocument doc, XmlElement parent, Punch punch)
        {
            var date = punch.InAt.ToString("yyyy-MM-dd");
            var customer = ReplaceCharacters(punch.Task.Job.Customer.Name);
            var job = ReplaceCharacters(punch.Task.Job.Name);
            var employee = ReplaceCharacters(punch.User.QuickBooksEmployee);
            TimeSpan span = punch.OutAt.Value.Subtract(punch.InAt);
            var duration = string.Format("PT{0}H{1}M", span.Hours, span.Minutes);
            var guid = string.Format("{{{0}}}", punch.Guid.ToString());
            var payrollItem = punch.Task.QuickBooksPayrollItem;
            var serviceItem = punch.Task.QuickBooksServiceItem;
            var task = ReplaceCharacters(punch.Task.Name);

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
            EntityRef.AppendChild(MakeSimpleElem(doc, "FullName", employee));

            // Create CustomerRef aggregate and fill in field values for it
            XmlElement CustomerRef = doc.CreateElement("CustomerRef");
            TimeTrackingAdd.AppendChild(CustomerRef);
            // Set field value for FullName
            CustomerRef.AppendChild(MakeSimpleElem(doc, "FullName", customer));

            // Create ItemServiceRef aggregate and fill in field values for it
            XmlElement ItemServiceRef = doc.CreateElement("ItemServiceRef");
            TimeTrackingAdd.AppendChild(ItemServiceRef);
            // Set field value for FullName
            ItemServiceRef.AppendChild(MakeSimpleElem(doc, "FullName", serviceItem));

            // Set field value for Duration
            TimeTrackingAdd.AppendChild(MakeSimpleElem(doc, "Duration", duration));

            //// Create ClassRef aggregate and fill in field values for it
            //XmlElement ClassRef = doc.CreateElement("ClassRef");
            //TimeTrackingAdd.AppendChild(ClassRef);
            //// Set field value for FullName
            //ClassRef.AppendChild(MakeSimpleElem(doc, "FullName", "ab"));
            
            // Create PayrollItemWageRef aggregate and fill in field values for it
            XmlElement PayrollItemWageRef = doc.CreateElement("PayrollItemWageRef");
            TimeTrackingAdd.AppendChild(PayrollItemWageRef);
            // Set field value for FullName
            PayrollItemWageRef.AppendChild(MakeSimpleElem(doc, "FullName", payrollItem));

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

        private async Task<List<Punch>> GetPunches()
        {
            ExportButtonStatus = "Getting Punches for Commit";
            OnPropertyChanged("ExportButtonStatus");

            // Build request to retrieve punches
            var request = new RestRequest(
                string.Format("odata/Punches?$expand=User,Task($expand=Job($expand=Customer))&$orderby=InAt&$filter=CommitId eq {0}", SelectedCommit.Id),
                Method.GET);

            // Execute request to retrieve punches
            var response = await client.ExecuteTaskAsync<ODataResponse<Punch>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                ExportButtonStatus = "Got Punches From BRIZBEE";
                OnPropertyChanged("ExportButtonStatus");

                return response.Data.Value;
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

        private string ReplaceCharacters(string value)
        {
            var replaced = value;

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

        private async System.Threading.Tasks.Task<Commit> UpdateCommit(Commit commit, DateTime exportedAt)
        {
            // Build the request
            var url = string.Format("odata/Commits({0})", commit.Id);
            var request = new RestRequest(url, Method.PATCH);
            request.AddJsonBody(new
            {
                QuickBooksExportedAt = exportedAt
            });

            // Execute request
            var response = await client.ExecuteTaskAsync<Commit>(request);
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

        private void WalkTimeTrackingAddRs(string response)
        {
            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(),
                responseXmlDoc.OuterXml,
                EventLogEntryType.Information);

            //Get the response for our request
            XmlNodeList TimeTrackingAddRsList = responseXmlDoc.GetElementsByTagName("TimeTrackingAddRs");
            if (TimeTrackingAddRsList.Count == 1) //Should always be true since we only did one request in this sample
            {
                XmlNode responseNode = TimeTrackingAddRsList.Item(0);
                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;
                
                //status code = 0 all OK, > 0 is warning
                if (Convert.ToInt32(statusCode) >= 0)
                {
                    XmlNodeList TimeTrackingRetList = responseNode.SelectNodes("//TimeTrackingRet");//XPath Query
                    for (int i = 0; i < TimeTrackingRetList.Count; i++)
                    {
                        XmlNode TimeTrackingRet = TimeTrackingRetList.Item(i);
                        WalkTimeTrackingRet(TimeTrackingRet);
                    }
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

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
