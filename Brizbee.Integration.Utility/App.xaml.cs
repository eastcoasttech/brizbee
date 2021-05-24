﻿//
//  App.xaml.cs
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

using Brizbee.Integration.Utility.Views;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        private NotifyIcon icon = new NotifyIcon();

        public App()
        {
            // Initialize MessageBus Using Dispatcher
            Action<Action> uiThreadMarshaller =
                action => Dispatcher.Invoke(DispatcherPriority.Normal, action);

            System.Windows.Application.Current.Properties["EventSource"] = "BRIZBEE Integration Utility";
            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Configure the tray icon.
            Assembly a = Assembly.GetExecutingAssembly();
            Stream st = a.GetManifestResourceStream("Brizbee.Integration.Utility.Images.favicon.ico");
            
            icon.Icon = new Icon(st);
            icon.Visible = true;
            icon.ShowBalloonTip(2000, "BRIZBEE Integration Utility", "Started", ToolTipIcon.Info);
            icon.MouseClick += icon_MouseClick;

            var strip = new ContextMenuStrip();
            strip.Items.Add("Open...");
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
            else if (item.Text == "Exit")
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            icon.Dispose();
        }
    }
}
