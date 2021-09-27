//
//  SendLogViewModel.cs
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

using Azure.Storage.Blobs;
using NLog;
using NLog.Targets;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Brizbee.Integration.Utility.ViewModels
{
    public class SendLogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsSendEnabled { get; set; }
        public string StatusText { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void Upload()
        {
            IsSendEnabled = false;
            StatusText = "Uploading...";
            OnPropertyChanged("IsSendEnabled");
            OnPropertyChanged("StatusText");

            try
            {
                var hostname = Environment.MachineName;
                var now = DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss");

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                var version = fvi.FileVersion;

                UriBuilder fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = string.Format("{0}.blob.core.windows.net", "ects1"),
                    Path = string.Format("{0}/{1}", "log-uploads", $"{now}_{version}_{hostname}.txt"),
                    Query = "sv=2020-08-04&ss=b&srt=o&sp=w&se=2022-09-01T04:00:00Z&st=2021-09-01T04:00:00Z&spr=https&sig=VCRkJLHLx41cadrggMXbE78l9I5hLX%2B1EzQJGI%2Fh5hs%3D"
                };

                var blobClient = new BlobClient(fullUri.Uri, null);

                var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName("logfile");
                var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
                string fileName = fileTarget.FileName.Render(logEventInfo);

                using (var uploadFileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    blobClient.Upload(uploadFileStream, true);
                    uploadFileStream.Close();
                }

                StatusText = "Uploaded";
                OnPropertyChanged("StatusText");
            }
            catch (Exception ex)
            {
                StatusText = ex.Message;
                OnPropertyChanged("StatusText");
            }
            finally
            {
                IsSendEnabled = true;
                OnPropertyChanged("IsSendEnabled");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
