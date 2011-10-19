//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------
 
namespace CoApp.Bootstrapper {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
    
      
        [STAThreadAttribute]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main( string[] args ) {
            SingleStep.OutputDebugString("Startup");
            // Ensure that we are elevated. If the app returns from here, we are.
            SingleStep.ElevateSelf(args);

            // get the folder of the bootstrap EXE
            SingleStep.BootstrapFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));

            if (args.Length == 0) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MISSING_MSI_FILE_ON_COMMANDLINE, "Missing MSI package name on command line!");
            } else if (!File.Exists(Path.GetFullPath(args[0]))) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_FOUND, "Specified MSI package name does not exist!");
            } else if (!SingleStep.ValidFileExists(Path.GetFullPath(args[0]))) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_VALID, "Specified MSI package is not signed with a valid certificate!");
            } else { // have a valid MSI file. Alrighty!
                SingleStep.MsiFilename = Path.GetFullPath(args[0]);
                SingleStep.MsiFolder = Path.GetDirectoryName(SingleStep.MsiFilename);

                // if this installer is present, this will exit right after.
                if (SingleStep.IsCoAppInstalled) {
                    SingleStep.RunInstaller(1);
                    return;
                }

                // if CoApp isn't there, we gotta get it.
                // this is a quick call, since it spins off a task in the background.
                //SingleStep.OutputDebugString("Start Installer");
                SingleStep.InstallCoApp();
            }

            // start showin' the GUI.
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
