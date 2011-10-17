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
    
        [DllImport("user32.dll")] 
        private static extern IntPtr GetForegroundWindow();

        private static string ExeName { get {
            var target =Assembly.GetEntryAssembly().Location;
            if (target.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) {
                return target;
            }
            // come up with a better EXE name that will work with ShellExecute
            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            target =  Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(fvi.FileName)
                ? (string.IsNullOrEmpty(fvi.ProductName) ? Path.GetFileName(target) : fvi.ProductName) : fvi.FileName)+".exe");
            File.Copy(Assembly.GetEntryAssembly().Location, target);
            return target;
        }}

        internal static void ElevateSelf(string[] args) {
            try {
                // I'm too lazy to check for admin priviliges, lets let the OS figure it out.
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"Software\CoApp\temp").SetValue("", DateTime.Now.Ticks);
            }
            catch {
                try {
                    new Process {
                        StartInfo = {
                            UseShellExecute = true,
                            WorkingDirectory = Environment.CurrentDirectory,
                            FileName = ExeName,
                            Verb = "runas",
                            Arguments = args[0],
                            ErrorDialog = true,
                            ErrorDialogParentHandle = GetForegroundWindow(),
                            WindowStyle = ProcessWindowStyle.Maximized,
                        }
                    }.Start();
                    Environment.Exit(0); // since this didn't throw, we know the kids got off to school ok. :)
                }
                catch {
                    Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_REQUIRES_ADMIN_RIGHTS, "The installer requires administrator permissions.");
                }
            }
        }


        internal static Lazy<App> Instance = new Lazy<App>(() => {
            var app = new App();
            app.InitializeComponent();
            return app;
        });
 
        [STAThreadAttribute]
        public static void Main( string[] args ) {
            // Ensure that we are elevated. If the app returns from here, we are.
            ElevateSelf(args);

            // get the folder of the bootstrap EXE
            Bootstrapper.MainWindow.BootstrapFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));

            if (args.Length == 0) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MISSING_MSI_FILE_ON_COMMANDLINE, "Missing MSI package name on command line!");
            } else if (!File.Exists(Path.GetFullPath(args[0]))) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_FOUND, "Specified MSI package name does not exist!");
            } else if (!Bootstrapper.MainWindow.ValidFileExists(Path.GetFullPath(args[0]))) {
                Bootstrapper.MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_VALID, "Specified MSI package is not signed with a valid certificate!");
            } else { // have a valid MSI file. Alrighty!
                Bootstrapper.MainWindow.MsiFilename = Path.GetFullPath(args[0]);
                Bootstrapper.MainWindow.MsiFolder = Path.GetDirectoryName(Bootstrapper.MainWindow.MsiFilename);

                // if this installer is present, this will exit right after.
                Bootstrapper.MainWindow.TryInstaller();

                // if CoApp isn't there, we gotta get it.
                // this is a quick call, since it spins off a task in the background.
                Bootstrapper.MainWindow.InstallCoApp();
            }

            // start showin' the GUI.
            Instance.Value.Run();
        }
    }
}
