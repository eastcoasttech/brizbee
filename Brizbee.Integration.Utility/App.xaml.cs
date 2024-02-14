//
//  App.xaml.cs
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

using Brizbee.Integration.Utility.Views;
using NLog;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Brizbee.Integration.Utility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private NotifyIcon icon = new();
        private Mutex _instanceMutex = null;

        public App()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Setup loggers.
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "file.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers.
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply configuration to logger.
            LogManager.Configuration = config;

            // Initialize MessageBus Using Dispatcher
            Action<Action> uiThreadMarshaller =
                action => Dispatcher.Invoke(DispatcherPriority.Normal, action);

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Configure the tray icon.
            var a = Assembly.GetExecutingAssembly();
            var st = a.GetManifestResourceStream("Brizbee.Integration.Utility.Images.favicon.ico");
            
            icon.Icon = new Icon(st);
            icon.Visible = true;
            icon.ShowBalloonTip(2000, "BRIZBEE Integration Utility", "Started", ToolTipIcon.Info);
            icon.MouseClick += icon_MouseClick;

            var strip = new ContextMenuStrip();
            strip.Items.Add("Open...");
            strip.Items.Add("-");
            strip.Items.Add("Send Log Files...");
            strip.Items.Add("-");
            strip.Items.Add("Exit");

            strip.ItemClicked += contexMenuStrip_ItemClicked;

            icon.ContextMenuStrip = strip;

            // Show the main window on first startup.
            MainWindow = new WizardWindow();
            MainWindow.Show();
            MainWindow.Focus();
        }

        void icon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                icon.ContextMenuStrip.Show();
            }
            else
            {
                MainWindow = new WizardWindow();
                MainWindow.Show();
                MainWindow.Focus();
            }
        }

        void contexMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var item = e.ClickedItem;

            if (item.Text == "Open...")
            {
                MainWindow = new WizardWindow();
                MainWindow.Show();
            }
            else if (item.Text == "Send Log Files...")
            {
                var sendLogWindow = new SendLogWindow();
                sendLogWindow.Show();
            }
            else if (item.Text == "Exit")
            {
                Current.Shutdown();
            }
        }

        void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            // Ensure icon is removed.
            icon.Dispose();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _instanceMutex = new Mutex(true, "BRIZBEE Integration Utility", out createdNew);
            if (!createdNew)
            {
                _instanceMutex = null;
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_instanceMutex != null)
                _instanceMutex.ReleaseMutex();

            // Ensure icon is removed.
            icon.Dispose();

            base.OnExit(e);
        }
    }
}
