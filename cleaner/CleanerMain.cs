//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------


namespace CoApp.Cleaner {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Win32;

    class CleanerMain {

        internal static bool AllPackages = true;
        internal static string StatusText = string.Empty;
        internal static string MessageText = "Press the OK button to continue with Cleanup\r\nPress CANCEL to exit.";
        internal static int OverallProgress = 0;

        private static CleanerMainWindow window;
        public static event Action PropertyChanged;

        /// <summary>
        /// Help message for the user
        /// </summary>
        private const string help =
            @"
Usage:
-------

CoApp.Cleaner [options] 
    
    Options:
    --------
    --help                      this help

    --just-coapp                remove just CoApp toolkit

    --quiet                     don't show the UI
    --auto                      don't show UI with cancel and ok buttons.
";
        private static string ExeName {
            get {
                var src = Assembly.GetEntryAssembly().Location;
                if (!src.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) {
                    var target = Path.Combine(Path.GetTempPath(), "Installer." + Process.GetCurrentProcess().Id + ".exe");
                    File.Copy(src, target);
                    return target;
                }
                return src;
            }
        }



        private static void OnPropertyChanged() {
            if (window != null && PropertyChanged != null) {
                window.Invoke(PropertyChanged);
            }
            
        }


        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiOpenPackageEx(string szPackagePath, uint dwOptions, out int hProduct);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiGetProperty(int hInstall, string szName, StringBuilder szValueBuf, ref uint cchValueBuf);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiCloseHandle(int hAny);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiGetProductInfo(string szProduct, string szProperty, StringBuilder lpValueBuf, ref uint pcchValueBuf);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiEnumProductsEx(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] int dwContext, uint dwIndex, StringBuilder szInstalledProductCode, [MarshalAs(UnmanagedType.I4)] out int pdwInstalledContext, StringBuilder szSid, ref uint pcchSid);

        [StructLayout(LayoutKind.Sequential)]
        public struct SidIdentifierAuthority {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.I1)]
            public byte[] Value;
        }

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiInstallProduct(string szPackagePath, string szCommandLine);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern uint MsiSetInternalUI(uint dwUILevel, IntPtr phWnd);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern NativeExternalUIHandler MsiSetExternalUI(
            [MarshalAs(UnmanagedType.FunctionPtr)] NativeExternalUIHandler puiHandler, uint dwMessageFilter, IntPtr pvContext);

        [DllImportAttribute("advapi32.dll", EntryPoint = "AllocateAndInitializeSid")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool AllocateAndInitializeSid([In] ref SidIdentifierAuthority pIdentifierAuthority, byte nSubAuthorityCount, uint nSubAuthority0, uint nSubAuthority1, uint nSubAuthority2, uint nSubAuthority3, uint nSubAuthority4, uint nSubAuthority5, int nSubAuthority6, uint nSubAuthority7, out IntPtr pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool CheckTokenMembership(IntPtr TokenHandle, IntPtr SidToCheck, out bool IsMember);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        internal static void ElevateSelf(string args) {
            try {
                var ntAuth = new SidIdentifierAuthority();
                ntAuth.Value = new byte[] { 0, 0, 0, 0, 0, 5 };

                var psid = IntPtr.Zero;
                bool isAdmin;
                if (AllocateAndInitializeSid(ref ntAuth, 2, 0x00000020, 0x00000220, 0, 0, 0, 0, 0, 0, out psid) && CheckTokenMembership(IntPtr.Zero, psid, out isAdmin) && isAdmin) {
                    return; // yes, we're an elevated admin
                }
            }
            catch {
                // :)
            }

            // we're not an admin I guess.
            try {
                new Process {
                    StartInfo = {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = ExeName,
                        Verb = "runas",
                        Arguments = args,
                        ErrorDialog = true,
                        ErrorDialogParentHandle = GetForegroundWindow(),
                        WindowStyle = ProcessWindowStyle.Maximized,
                    }
                }.Start();
                Environment.Exit(0); // since this didn't throw, we know the kids got off to school ok. :)
            }
            catch {
                Fail("This tool requires administrator permissions.");
                Environment.Exit(1); 
            }
        }

        internal delegate int NativeExternalUIHandler(IntPtr context, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);
        private static NativeExternalUIHandler uihandler;
        
        private static int _progressDirection = 1;
        private static int _currentTotalTicks = -1;
        private static int _currentProgress;
        private static int ActualPercent;

        internal static int UiHandler(IntPtr context, int messageType, string message) {
            if ((0xFF000000 & (uint)messageType) == 0x0A000000 && message.Length >= 2) {
                int i;
                var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(each => Int32.TryParse(each, out i) ? i : 0).ToArray();

                switch (msg[1]) {
                        // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                    case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                        _currentTotalTicks = msg[3];
                        _currentProgress = 0;
                        if (msg.Length >= 6) {
                            _progressDirection = msg[5] == 0 ? 1 : -1;
                        }
                        break;
                    case 1:
                        //Provides information related to progress messages to be sent by the current action.
                        break;
                    case 2: //Increments the progress bar.
                        if (_currentTotalTicks == -1) {
                            break;
                        }
                        _currentProgress += msg[3] * _progressDirection;
                        break;
                    case 3:
                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                        break;
                }
            }

            if (_currentTotalTicks > 0) {
                // this will only return #s between 10 and 80. The last 20% of progress is for warmup.
                ActualPercent = (_currentProgress * 100 / _currentTotalTicks);
            }

            // if the cancel flag is set, tell MSI
            return 1;
        }

        private static string[] args;

        [STAThread]
        static void Main(string[] arguments) {
            if (arguments.Contains("--help")) {
                MessageBox.Show(help);
                return;
            }

            ElevateSelf(arguments.Aggregate(string.Empty, (current, each) => current + " " + each).Trim());
            args = arguments;

            if (args.Contains("--just-coapp")) {
                AllPackages = false;
            }

            if (!args.Contains("--quiet")) {
                // we're going to show the UI
                window = new CleanerMainWindow();

                if( args.Contains("--auto")) {
                    window.Height = 250;
                    window.started = true;

                    // start the wiper
                    window.started = true;
                    Task.Factory.StartNew(Start);
                }
                window.ShowDialog();
            } else {
                Start();
            }
        }

        public static void Start() {
            try {
                MessageText = "It will be just a moment while we \r\nremove old versions of the\r\nCoApp Package Manager.";
                StatusText = "Status: Shutting down CoApp Service";
                OverallProgress = 5;
                OnPropertyChanged();

                // try to gracefully kill coapp.service
                try {
                    var psi = new ProcessStartInfo {
                        FileName = "sc.exe",
                        Arguments = @"stop  ""CoApp Package Installer Service""",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var p = Process.Start(psi);
                    p.WaitForExit();
                } catch {
                    // it's ok.
                }

                // let's just kill the processes if they exist
                var serviceProcs = Process.GetProcessesByName("CoApp.Service");
                if (serviceProcs.Any()) {
                    foreach (var proc in serviceProcs) {
                        try {
                            proc.Kill();
                        } catch {
                            // it's ok.
                        }
                    }
                }

                StatusText = "Status: Removing Service";
                OverallProgress = 10;
                OnPropertyChanged();
                // remove service if it exists
                try {
                    var psi = new ProcessStartInfo {
                        FileName = "sc.exe",
                        Arguments = @"delete  ""CoApp Package Installer Service""",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var p = Process.Start(psi);
                    p.WaitForExit();
                } catch {
                    // it's ok.
                }

                MsiSetInternalUI(2, IntPtr.Zero);
                MsiSetExternalUI((context, messageType, message) => 1, 0x400, IntPtr.Zero);

                StatusText = "Status: Finding installed packages.";
                OverallProgress = 15;
                OnPropertyChanged();

                var installedMSIs = GetInstalledCoAppMSIs().ToArray();
                StatusText = string.Format("Status: Found {0} installed packages.", installedMSIs.Length);
                OverallProgress = 20;
                OnPropertyChanged();

                // Remove CoApp toolkit MSIs
                var toolkits = installedMSIs.Where(each => each.ProductName == "CoApp.Toolkit" && each.Manufacturer == "OUTERCURVE FOUNDATION").ToArray();

                if (toolkits.Any()) {
                    StatusText = "Status: Removing CoApp Toolkit.";
                    OverallProgress = 25;
                    OnPropertyChanged();

                    foreach (var pkg in toolkits) {
                        OverallProgress++;
                        OnPropertyChanged();

                        MsiInstallProduct(pkg.Path, @"REMOVE=ALL ALLUSERS=1 COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
                    }
                }

                if (installedMSIs.Any()) {
                    installedMSIs = GetInstalledCoAppMSIs().ToArray();

                    if (AllPackages && installedMSIs.Any()) {
                        var eachProgress = 45/installedMSIs.Count();
                        StatusText = "Status: Removing other packages.";
                        OverallProgress = 30;
                        OnPropertyChanged();

                        foreach (var pkg in installedMSIs) {
                            OverallProgress += eachProgress;
                            OnPropertyChanged();

                            MsiInstallProduct(pkg.Path, @"REMOVE=ALL ALLUSERS=1 COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
                        }
                    }


                    installedMSIs = GetInstalledCoAppMSIs().ToArray();
                    if (!installedMSIs.Any()) {
                        StatusText = "Status: Removing CoApp Folder.";
                        OverallProgress = 75;
                        OnPropertyChanged();
                        // no more packages installed-- remove the c:\apps directory
                        var apps = CoAppRootFolder.Value;
                        TryHardToDelete(apps);
                    }

                }
                // clean out the CoApp registry keys
                try {
                    StatusText = "Status: Removing CoApp Registry Settings.";
                    OverallProgress = 83;
                    OnPropertyChanged();

                    var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"Software");
                    if (registryKey != null) {
                        registryKey.DeleteSubKeyTree("CoApp");
                    }
                } catch {
                }

                StatusText = "Status: Cleaning up Temp Folder.";
                OverallProgress = 85;
                OnPropertyChanged();


                foreach (var f in Directory.EnumerateFiles(TempPath, "*.msi")) {
                    TryHardToDelete(f);
                }
                OverallProgress = 88;
                OnPropertyChanged();

                foreach (var f in Directory.EnumerateFiles(TempPath, "*.tmp")) {
                    TryHardToDelete(f);
                }

                OverallProgress = 91;
                OnPropertyChanged();

                foreach (var f in Directory.EnumerateFiles(TempPath, "*.exe")) {
                    TryHardToDelete(f);
                }
                OverallProgress = 93;
                OnPropertyChanged();

                foreach (var f in Directory.EnumerateFiles(TempPath, "*.dll")) {
                    TryHardToDelete(f);
                }

                OverallProgress = 95;
                OnPropertyChanged();


                MsiSetExternalUI(null, 0x400, IntPtr.Zero);
                RemoveTemporaryFiles();

                StatusText = "Status: Complete";
                OverallProgress = 100;
                OnPropertyChanged();
            } catch {
                // meh.
            }

            Environment.Exit(0);
        }

        private void Remove(string file) {
            MsiInstallProduct(file, @"REMOVE=ALL ALLUSERS=1 COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS");
        }


        private static string GetRegistryValue(string key, string valueName) {
            try {
                var openSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(key);
                if (openSubKey != null) {
                    return openSubKey.GetValue(valueName).ToString();
                }
            }
            catch {
            }
            return null;
        }

        internal static readonly Lazy<string> CoAppRootFolder = new Lazy<string>(() => {
            var result = GetRegistryValue(@"Software\CoApp", "Root");

            if (String.IsNullOrEmpty(result)) {
                result = String.Format("{0}\\apps", Environment.GetEnvironmentVariable("SystemDrive"));
            }
            return result;
        });

        private static string _tempPath;
        private static string TempPath {
            get {
                if (string.IsNullOrEmpty(_tempPath)) {
                    _tempPath = Path.GetTempPath();
                    if (!Directory.Exists(_tempPath)) {
                        Directory.CreateDirectory(_tempPath);
                    }
                }
                return _tempPath;
            }
        }

        private static int _counter = Process.GetCurrentProcess().Id << 16;

        public static int Counter { get { return ++_counter; } }
        public static string CounterHex { get { return Counter.ToString("x8"); } }

        private static List<string> _disposableFilenames = new List<string>();

        internal enum MoveFileFlags {
            MOVEFILE_REPLACE_EXISTING = 1,
            MOVEFILE_COPY_ALLOWED = 2,
            MOVEFILE_DELAY_UNTIL_REBOOT = 4,
            MOVEFILE_WRITE_THROUGH = 8
        }

        [DllImportAttribute("kernel32.dll", EntryPoint = "MoveFileEx")]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);


        public static string MarkFileTemporary(string filename) {
            _disposableFilenames.Add(filename);
            return filename;
        }

        public static string GenerateTemporaryFilename(string filename) {

            string ext = null;
            string name = null;
            string folder = null;

            if (!string.IsNullOrEmpty(filename)) {
                ext = Path.GetExtension(filename);
                name = Path.GetFileNameWithoutExtension(filename);
                folder = Path.GetDirectoryName(filename);
            }

            if (string.IsNullOrEmpty(ext)) {
                ext = ".tmp";
            }
            if (string.IsNullOrEmpty(folder)) {
                folder = TempPath;
            }

            name = Path.Combine(folder, "tmpFile." + CounterHex + (string.IsNullOrEmpty(name) ? ext : "." + name + ext));

            if (File.Exists(name)) {
                TryHardToDelete(name);
            }

            return MarkFileTemporary(name);
        }

        public static void RemoveTemporaryFiles() {
            foreach (var f in _disposableFilenames.ToArray().Where(File.Exists)) {
                TryHardToDelete(f);
            }
        }

        public static void TryHardToDelete(string location) {
            if (Directory.Exists(location)) {
                try {
                    Directory.Delete(location, true);
                }
                catch {
                    // didn't take, eh?
                }
            }

            if (File.Exists(location)) {
                try {
                    File.Delete(location);
                }
                catch {
                    // didn't take, eh?
                }
            }

            // if it is still there, move and mark it.
            if (File.Exists(location) || Directory.Exists(location)) {
                try {
                    // move the file to the tmp file
                    // and tell the OS to remove it next reboot.
                    var tmpFilename = GenerateTemporaryFilename(location); // generates a unique filename but not a file!
                    MoveFileEx(location, tmpFilename, MoveFileFlags.MOVEFILE_REPLACE_EXISTING);

                    if (File.Exists(location) || Directory.Exists(location)) {
                        // of course, if the tmpFile isn't on the same volume as the location, this doesn't work.
                        // then, last ditch effort, let's rename it in the current directory
                        // and then we can hide it and mark it for cleanup .
                        tmpFilename = Path.Combine(Path.GetDirectoryName(location), "tmp." + CounterHex + "." + Path.GetFileName(location));
                        MoveFileEx(location, tmpFilename, MoveFileFlags.MOVEFILE_REPLACE_EXISTING);
                        if (File.Exists(tmpFilename) || Directory.Exists(location)) {
                            // hide the file for convenience.
                            File.SetAttributes(tmpFilename, File.GetAttributes(tmpFilename) | FileAttributes.Hidden);
                        }
                    }

                    // Now we mark the locked file to be deleted upon next reboot (or until another coapp app gets there)
                    MoveFileEx(File.Exists(tmpFilename) ? tmpFilename : location, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch (Exception e) {
                    // really. Hmmm. 
                    // you can log this if you want.
                }

                if (File.Exists(location)) {
                    // Unable to forcably remove file?
                    // get angry!?
                }
            }
            return;
        }



        #region fail/help

        /// <summary>
        /// Print an error to the console
        /// </summary>
        /// <param name="text">An error message</param>
        /// <param name="par">A format string</param>
        /// <returns>Always returns 1</returns>
        /// <seealso cref="String.Format(string, object[])"/>
        /// <remarks>
        /// Format according to http://msdn.microsoft.com/en-us/library/b1csw23d.aspx
        /// </remarks>
        public static int Fail(string text, params object[] par) {
            MessageBox.Show("Error:{0}", string.Format(text, par));
            return 1;
        }

        /// <summary>
        /// Print usage notes (help) and logo
        /// </summary>
        /// <returns>Always returns 0</returns>
        private static int Help() {
            MessageBox.Show(help);
            return 0;
        }

        #endregion

        internal static string GetProductInfo(string productCode, string propertyName) {
            var outputValue = new StringBuilder(2048);
            var sz = (uint)outputValue.Capacity;

            MsiGetProductInfo(productCode, propertyName, outputValue, ref sz);
            if( sz > 0 && outputValue.Length > 0 ) {
                return outputValue.ToString();
            }
            return null;
        }

        internal static IEnumerable<Product> GetInstalledCoAppMSIs() {
            var productCode = new StringBuilder(40);
            var localPackage = new StringBuilder(512);
            var sidBuf = new StringBuilder(40);

            for (uint i = 0; ; i++) {
                var sidBufSize = (uint)sidBuf.Capacity;
                int targetContext;
                var ret = MsiEnumProductsEx( null, null, 7, i, productCode, out targetContext, sidBuf, ref sidBufSize);

                if (ret == 234) {
                    sidBuf.Capacity = (int)++sidBufSize;
                    ret = MsiEnumProductsEx( null, null, 7, i, productCode, out targetContext, sidBuf, ref sidBufSize);
                }

                if (ret == 259) {
                    break;
                }

                var path = GetProductInfo(productCode.ToString(), "LocalPackage");
                var publisher = GetProductInfo(productCode.ToString(), "Publisher");

                if( string.IsNullOrEmpty(path) || string.IsNullOrEmpty(publisher)) {
                    continue;
                }

                var feed = GetMsiProperty(path, "CoAppPackageFeed");

                if(!string.IsNullOrEmpty(feed)) {
                    yield return new Product {
                        Path = path,
                        Publisher = publisher,
                        IsCoAppPackage = true,
                        ProductName = GetMsiProperty(path, "ProductName"),
                        Manufacturer = GetMsiProperty(path, "Manufacturer")
                    };    
                }

            }
        }


        internal static string GetMsiProperty(string filename, string propertyName) {
            if (!File.Exists(filename)) {
                return null;
            }

            var outputValue = new StringBuilder(2048);
            var sz = (uint)outputValue.Capacity;

            int hProduct;
            if (MsiOpenPackageEx(filename, 1, out hProduct) == 0) {
                MsiGetProperty(hProduct, propertyName, outputValue, ref sz);
                MsiCloseHandle(hProduct);
                if (sz > 0 && outputValue.Length > 0) {
                    return outputValue.ToString();
                }
            }

            return null;
        }
    }


    internal class Product {
        internal string Path;
        internal string Publisher;
        internal string ProductName;
        internal string Manufacturer;
        internal bool IsCoAppPackage;
    }
}
