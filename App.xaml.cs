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
    using System.Threading.Tasks;
    using System.Windows;
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
                catch (Exception e) {
                    // user cancelled the elevation.
                    // MessageBox.Show("Admin Priviliges are required.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Error,MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification );
                    // Environment.Exit(0);
                    Task.Factory.StartNew(() => Bootstrapper.MainWindow.Fail(Bootstrapper.MainWindow.IDS_REQUIRES_ADMIN_RIGHTS, "The installer requires administrator permissions."));
                }
            }
        }


        internal static Lazy<App> Instance = new Lazy<App>(() => {
            var app = new App();
            app.InitializeComponent();
            return app;
        });

        [DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);
        
        [System.STAThreadAttribute]
        public static void Main( string[] args ) {
            ElevateSelf(args);

            File.Delete("coapp.resources.dll");
            Bootstrapper.MainWindow.BootstrapFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));

            if (args.Length == 0 || !File.Exists(Path.GetFullPath(args[0])) ) {
                Task.Factory.StartNew(() => Bootstrapper.MainWindow.Fail(Bootstrapper.MainWindow.IDS_MISSING_MSI_FILE_ON_COMMANDLINE, "Missing MSI package name on command line!"));
            }
            else {
                Bootstrapper.MainWindow.MsiFilename = Path.GetFullPath(args[0]);
                Bootstrapper.MainWindow.MsiFolder = Path.GetDirectoryName(Bootstrapper.MainWindow.MsiFilename);

                if (Bootstrapper.MainWindow.IsCoAppInstalled) {
                    Bootstrapper.MainWindow.RunInstaller();
                    return;
                }
                Bootstrapper.MainWindow.InstallCoApp();
            }
            
            Instance.Value.Run();
        }
    }
}
