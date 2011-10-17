//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Engine.Client {
    using System;
    using System.Reflection;
    using System.Windows;
    using UI;
    using MessageBox = System.Windows.Forms.MessageBox;

    public class Installer : MarshalByRefObject {
        public class InstallerApp : Application {
        }

        public Installer(string filename) {
            // we'll take it from here...
            try {
                // First, see if the CoApp Service is running. If it's not, let's get it up and running.
                // this will throw an exception if it's just not possible.
#if DEBUG
                EngineServiceManager.EnsureServiceIsResponding(true);
#else
                  EngineServiceManager.EnsureServiceIsResponding();
#endif

                // if we got this far, CoApp must be running. 

                // Next check to see what the appropriate installer client is. If there isn't a default set, we'll
                // assume we are handling it here (as the Installer UI)
                // var mainWindow = new InstallerMainWindow();
                // MessageBox.Show("CoApp Service Should be running. Try to install \r\n" + filename, filename);

                // check to see if there are updates for CoApp itself that it should run first.
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
                // var app = new InstallerApp();
                // new Uri("/WpfApplication1;component/mainwindow.xaml", UriKind.Relative)
                // app.StartupUri = new Uri("InstallerMainWindow.xaml", UriKind.Relative);
                var window = new InstallerMainWindow(filename);
                window.ShowDialog();
            } catch (Exception e) {
                MessageBox.Show(e.StackTrace, e.Message);
            }
        }
    }
}