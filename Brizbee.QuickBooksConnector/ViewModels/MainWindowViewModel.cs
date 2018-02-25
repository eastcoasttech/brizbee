using Brizbee.Common.Models;
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
            var now = DateTime.Now;
            var guid = Guid.NewGuid();

            OdbcConnection conn = new OdbcConnection("");
            conn.Open();
            
            var punches = GetPunches().Result;

            foreach (var punch in punches)
            {
                var date = punch.InAt.ToString("yyyy-MM-dd");
                var customer = punch.Task.Job.Customer.Name.Replace("'", "''");
                var employee = punch.User.Name.Replace("'", "''");
                var minutes = punch.Minutes;
                var payrollItem = punch.QuickBooksPayrollItem;
                var serviceItem = punch.QuickBooksServiceItem;
                var task = punch.Task.Name.Replace("'", "''");

                OdbcCommand cmdInsert = conn.CreateCommand();
                cmdInsert.CommandText = string.Format("INSERT INTO TimeTracking (DurationMinutes, TxnDate, CustomerRefFullName, ItemServiceRefFullName, PayrollItemWageRefFullName, EntityRefFullName) Values ({0},{{d'{1}'}},'{2}','{3}','{4}','{5}')",
                    minutes,
                    date,
                    string.Format("{0}:{1} - {2}", customer, punch.Task.Job.Number, punch.Task.Job.Name.Replace("'", "''")),
                    serviceItem,
                    payrollItem,
                    employee);
                cmdInsert.ExecuteNonQuery();

                OdbcCommand cmdRead = conn.CreateCommand();
                cmdRead.CommandText = "sp_lastinsertid TimeTracking";
                OdbcDataReader rdr = cmdRead.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                rdr.Read();

                t.ImportedAt = now;
                t.ImportedTxnID = rdr.GetString(0);
                t.ImportedBatchId = guid.ToString();

                rdr.Close();
                cmdInsert.Dispose();
                cmdRead.Dispose();

                conn.Close();
                conn.Dispose();
            }








            //var client = new RestClient("http://app.brizbee.com/");
            //var request = new RestRequest("odata/Punches?$expand=Item,Item/WorkOrder,Item/WorkOrder/Job,User&$orderby={orderby}&$filter=(((InAt ge datetime'{from}') and (InAt le datetime'{to}')) or (OutAt eq null)){userQueryString}", Method.GET);
            //request.AddUrlSegment("orderby", grdTimesSortedColumnName);
            //request.AddUrlSegment("from", from.ToString("yyyy-MM-ddT00:00:00"));
            //request.AddUrlSegment("to", to.ToString("yyyy-MM-ddT23:59:59"));

            //client.ExecuteAsync<List<Punch>>(request, response =>
            //{
            //    try
            //    {
            //        this.Invoke((Action)(() =>
            //        {
            //            if ((response.ResponseStatus == ResponseStatus.Completed) &&
            //                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            //            {
            //                blTimes.Clear();
            //                foreach (Time time in response.Data)
            //                {
            //                    blTimes.Add(time);
            //                }
            //                grdTimes.Columns[grdTimesSortedColumnIndex].HeaderCell.SortGlyphDirection = grdTimesSortOrder;

            //                // update total hours
            //                Decimal total = grdTimes.Rows.Cast<DataGridViewRow>()
            //                    .Sum(t => Convert.ToDecimal(t.Cells["ColumnMinutes"].Value));
            //                lblHours.Text = string.Format("{0} Hours", total.ToString());
            //            }
            //            else
            //            {
            //                MessageBox.Show(this, response.StatusDescription, "Oops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //            }

            //            prgStatus.Visible = false;
            //        }));
            //    }
            //    catch (InvalidOperationException) { } // Form was closed
            //});
        }

        public async System.Threading.Tasks.Task RefreshCommits()
        {
            // Build request to retrieve commits
            var request = new RestRequest("odata/Commits?$orderby=InAt", Method.GET);

            // Execute request to retrieve authenticated user
            var response = await client.ExecuteTaskAsync<List<Commit>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Commits = new ObservableCollection<Commit>(response.Data);
                IsEnabled = true;
                SelectedCommit = Commits[0];
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("SelectedCommit");
            }
            else
            {
                Commits = new ObservableCollection<Commit>();
                IsEnabled = true;
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsEnabled");
            }
        }

        private async Task<List<Punch>> GetPunches()
        {
            // Build request to retrieve punches
            var request = new RestRequest(
                string.Format("odata/Punches?$expand=Task,Task/Job,Task/Job/Customer,User&$orderby=InAt&$filter=CommitId eq '{0}'", SelectedCommit.Id),
                Method.GET);

            // Execute request to retrieve punches
            var response = await client.ExecuteTaskAsync<List<Punch>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                return response.Data;
            }
            else
            {
                return new List<Punch>();
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
