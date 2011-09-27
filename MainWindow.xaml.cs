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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Win32;

    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
// ReSharper disable InconsistentNaming
        internal const int IDS_ERROR_CANT_OPEN_PACKAGE = 500;

        internal const int IDS_MISSING_MSI_FILE_ON_COMMANDLINE = 501;
        internal const int IDS_REQUIRES_ADMIN_RIGHTS = 502;
        internal const int IDS_SOMETHING_ODD = 503;
        internal const int IDS_FRAMEWORK_INSTALL_CANCELLED = 504;
        internal const int IDS_UNABLE_TO_DOWNLOAD_FRAMEWORK = 505;
        internal const int IDS_UNABLE_TO_FIND_SECOND_STAGE = 506;
        internal const int IDS_MAIN_MESSAGE = 507;
        internal const int IDS_CANT_CONTINUE = 508;
        internal const int IDS_FOR_ASSISTANCE = 509;
        internal const int IDS_OK_TO_CANCEL = 510;
        internal const int IDS_CANCEL = 511;
        internal const int IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER = 512;
        internal const int IDS_UNABLE_TO_LOCATE_INSTALLER_UI = 513;
// ReSharper restore InconsistentNaming

        internal static string MsiFilename;
        internal static string MsiFolder;
        internal static string BootstrapFolder;

        private const string CoAppUrl = "http://coapp.org/resources/";
        private const string HelpUrl = "http://coapp.org/help/";
        
        private static int _progressDirection = 1;
        private static int _currentTotalTicks = -1;
        private static int _currentProgress;
        private static int _actualPercent;

        internal static bool _cancelling;
        internal static MainWindow _mainwindow;
        private static readonly Lazy<string> _bootstrapServerUrl = new Lazy<string>(() => GetRegistryValue(@"Software\CoApp", "BootstrapServerUrl"));
        static internal Task InstallTask;
        private static readonly Lazy<NativeResourceModule> _resources = new Lazy<NativeResourceModule>(() => {
            try {
                return new NativeResourceModule(AcquireFile("coapp.resources.dll"));
            }
            catch {
                return null;
            }
        }, LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<string> _coAppRootFolder = new Lazy<string>(() => {
            var result = GetRegistryValue(@"Software\CoApp", "Root");
            if (String.IsNullOrEmpty(result)) {
                result = String.Format("{0}\\apps", Environment.GetEnvironmentVariable("SystemDrive"));
                SetRegistryValue(@"Software\CoApp", "Root", result);
            }
            if (!Directory.Exists(result)) {
                Directory.CreateDirectory(result);
            }
            return result;
        });

        public MainWindow() {
            InitializeComponent();
            Opacity = 0;
            if (_resources.Value != null) {
                containerPanel.Background.SetValue(ImageBrush.ImageSourceProperty, _resources.Value.GetBitmapImage(1201));
                logoImage.SetValue(Image.SourceProperty, _resources.Value.GetBitmapImage(1202));
            }
            
            Loaded += (o, e) => { _mainwindow = this; };
        }

        internal static bool IsCoAppInstalled {
            get {
                try {
                    // we're going to update this version, only when absolutely required.
                    Assembly.Load("CoApp.Toolkit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=820d50196d4e8857");
                    return true;
                }
                catch {
                }
                return false;
            }
        }

        private static string InstallerUiPath {
            get {
                string path;

                // check the PreferredInstallerPath in the registry
                try {
                    path = GetRegistryValue(@"Software\CoApp", "PreferredInstaller");
                    if (ValidFileExists(path)) {
                        return path;
                    }
                }
                catch {
                }
                SetRegistryValue(@"Software\CoApp", "PreferredInstaller", null);

                // check the DefaultInstallerPath in the registry
                try {
                    path = GetRegistryValue(@"Software\CoApp", "DefaultInstaller");
                    if (ValidFileExists(path)) {
                        return path;
                    }
                }
                catch {
                }
                SetRegistryValue(@"Software\CoApp", "DefaultInstaller", null);

                // look in the PATH
                try {
                    path = FindInPath("CoApp.InstallerUI.exe");
                    if (!string.IsNullOrEmpty(path)) {
                        // remember this location, it was found in a good spot.
                        SetRegistryValue(@"Software\CoApp", "DefaultInstaller", path);
                        return path;
                    }
                }
                catch {
                }

                // look in the $COAPPROOT\bin
                try {
                    path = Path.Combine(_coAppRootFolder.Value, "CoApp.InstallerUI.exe");
                    if (ValidFileExists(path)) {
                        // remember this location, it was found in a good spot.
                        SetRegistryValue(@"Software\CoApp", "DefaultInstaller", path);
                        return path;
                    }
                }
                catch {
                }

                // look in the $COAPPROOT\.installed\OuterCurve Foundation\* folders 
                try {
                    path =
                        (from each in
                            Directory.EnumerateFiles(Path.Combine(_coAppRootFolder.Value, ".installed\\Outercurve Foundation"), "CoApp.InstallerUI.exe",
                                SearchOption.AllDirectories)
                            let version = Version(each)
                            where ValidFileExists(each)
                            orderby version descending
                            select each).FirstOrDefault();

                    if (!string.IsNullOrEmpty(path)) {
                        // remember this location, it was found in a good spot.
                        SetRegistryValue(@"Software\CoApp", "DefaultInstaller", path);
                        return path;
                    }
                }

                catch {

                }
                return null;
            }
        }

        private static string AcquireFile(string filename, Action<int> progressCompleted = null) {
            OutputDebugString(string.Format("==> Trying to acquire {0}", filename));

            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            var lcid = CultureInfo.CurrentCulture.LCID;
            var localizedName = String.Format("{0}.{1}{2}", name, lcid, extension);
            string f;

            // is the localized file in the bootstrap folder?
            if (!string.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, localizedName);
                if (ValidFileExists(f)) {
                    return f;
                }
            }

            // is the localized file in the msi folder?
            if (!string.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, localizedName);
                if (ValidFileExists(f)) {
                    return f;
                }
            }
            // try the MSI for the localized file 
            f = GetFileFromMSI(localizedName);
            if (ValidFileExists(f)) {
                return f;
            }
            
            //------------------------
            // NORMAL FILE, ON BOX
            //------------------------

            // is the standard file in the bootstrap folder?
            if (!string.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, filename);
                if (ValidFileExists(f)) {
                    return f;
                }
            }

            // is the standard file in the msi folder?
            if (!string.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, filename);
                if (ValidFileExists(f)) {
                    return f;
                }
            }
            // try the MSI for the regular file 
            f = GetFileFromMSI(filename);
            if (ValidFileExists(f)) {
                return f;
            }

            //------------------------
            // LOCALIZED FILE, REMOTE
            //------------------------

            // try localized file off the bootstrap server
            if (!String.IsNullOrEmpty(_bootstrapServerUrl.Value)) {
                f = Download(_bootstrapServerUrl.Value, localizedName,progressCompleted);
                if (ValidFileExists(f)) {
                    return f;
                }
            }

            // try localized file off the coapp server
            f = Download(CoAppUrl, localizedName,progressCompleted);
            if (ValidFileExists(f)) {
                return f;
            }

            // try normal file off the bootstrap server
            if (!String.IsNullOrEmpty(_bootstrapServerUrl.Value)) {
                f = Download(_bootstrapServerUrl.Value, filename,progressCompleted);
                if (ValidFileExists(f)) {
                    return f;
                }
            }

            // try normal file off the coapp server
            f = Download(CoAppUrl, filename,progressCompleted);
            if (ValidFileExists(f)) {
                return f;
            }

            return null;
        }

        [DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);

        private static string Download(string serverUrl, string filename, Action<int> percentCompleteAction = null) {
            try {
                OutputDebugString(string.Format("Trying to HEAD {0}", filename));
                // the whole URL to the target download
                var uri = new Uri(new Uri(serverUrl), filename);

                // we'll first check to see if the file is there with HEAD 
                // since getting errors back from WebClient is virtually impossible.

                var rq = WebRequest.Create(uri);
                
                rq.Method = "HEAD";
                rq.GetResponse(); // this will throw on anything other than OK

                OutputDebugString(string.Format("Trying to actually download {0}", filename));

                var finished = new ManualResetEvent(false);
                
                var tempFilenme = Path.Combine(Path.GetTempPath(), filename);
                if (File.Exists(tempFilenme)) {
                    File.Delete(tempFilenme);
                }

                var webclient = new WebClient();

                webclient.DownloadProgressChanged += (o, downloadProgressChangedEventArgs) => {
                    if( _cancelling ) {
                        webclient.CancelAsync();
                        return;
                    }

                    if( percentCompleteAction != null  ) {
                        percentCompleteAction(downloadProgressChangedEventArgs.ProgressPercentage);
                    }
                };

                webclient.DownloadFileCompleted += (o, asyncCompletedEventArgs ) => {
                    if( asyncCompletedEventArgs.Cancelled || asyncCompletedEventArgs.Error!=null  ) {
                        if (File.Exists(tempFilenme)) {
                            File.Delete(tempFilenme);
                        }
                    }
                    finished.Set();
                };

                webclient.DownloadFileAsync(uri, tempFilenme);

                while(!finished.WaitOne(100)) {
                    if( _cancelling) {
                        webclient.CancelAsync();
                        return null;
                    }
                }

                if (File.Exists(tempFilenme)) {
                    return tempFilenme;
                }
            }
            catch {
            }
            OutputDebugString(string.Format("Failed to download {0}", filename));
            return null;
        }

        private static string GetFileFromMSI(string binaryFile) {
            var packageDatabase = 0;
            var view = 0;
            var record = 0;

            if (String.IsNullOrEmpty(MsiFilename)) {
                return null;
            }

            try {
                if (0 != NativeMethods.MsiOpenDatabase(binaryFile, IntPtr.Zero, out packageDatabase)) {
                    return null;
                }
                if (0 != NativeMethods.MsiDatabaseOpenView(packageDatabase, String.Format("SELECT `Data` FROM `Binary` where `Name`='{0}'", binaryFile), out view)) {
                    return null;
                }
                if (0 != NativeMethods.MsiViewExecute(view, 0)) {
                    return null;
                }
                if (0 != NativeMethods.MsiViewFetch(view, out record)) {
                    return null;
                }

                var bufferSize = NativeMethods.MsiRecordDataSize(record, 1);
                if (bufferSize > 1024*1024*1024 || bufferSize == 0) {
                    //bigger than 1Meg?
                    return null;
                }

                var byteBuffer = new byte[bufferSize];

                if (0 != NativeMethods.MsiRecordReadStream(record, 1, byteBuffer, ref bufferSize)) {
                    return null;
                }

                // got the whole file
                var tempFilenme = Path.Combine(Path.GetTempPath(), binaryFile);
                File.WriteAllBytes(tempFilenme, byteBuffer);
                return tempFilenme;
            }
            finally {
                if (record != 0) {
                    NativeMethods.MsiCloseHandle(record);
                }
                if (view != 0) {
                    NativeMethods.MsiCloseHandle(view);
                }
                if (packageDatabase != 0) {
                    NativeMethods.MsiCloseHandle(packageDatabase);
                }
            }
        }

        private static bool ValidFileExists(string fileName) {
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName)) {
#if DEBUG
                return true;
#else
                try {
                    var wtd = new WinTrustData(fileName);
                    var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                    return (result == WinVerifyTrustResult.Success);
                }
                catch {

                }
#endif
            }
            return false;
        }

        public static void Fail(uint message, string messageText ) {
            if (!_cancelling) {
                _cancelling = true;
                messageText = GetString(message, messageText);

                while( _mainwindow == null ) {
                    Thread.Sleep(50);
                }

                Invoke(() => {
                    
                    _mainwindow.containerPanel.Background = new SolidColorBrush(new Color {
                        A = 255,
                        R = 18,
                        G = 112,
                        B = 170
                    });
                    _mainwindow.progressPanel.Visibility = Visibility.Collapsed;
                    _mainwindow.failPanel.Visibility = Visibility.Visible;
                    _mainwindow.messageText.Text = messageText;
                    _mainwindow.helpLink.NavigateUri =new Uri(HelpUrl + (message+100));
                    _mainwindow.helpLink.Inlines.Clear();
                    _mainwindow.helpLink.Inlines.Add(new Run(HelpUrl + (message+100)));
                    _mainwindow.Opacity = 1;
                    });
                
            }
        }

        protected internal static void Invoke(Action action) {
            _mainwindow.Dispatcher.Invoke(action);
        }

        private static string FindInPath(string filename) {
            try {
                var s = new StringBuilder(260); // MAX_PATH
                IntPtr p;

                NativeMethods.SearchPath(null, filename, null, s.Capacity, s, out p);
                var result = s.ToString();
                if (ValidFileExists(result)) {
                    return result;
                }
            }
            catch {
            }
            return string.Empty;
        }


        public static void RunInstaller() {
            var installer = InstallerUiPath;
            if( string.IsNullOrEmpty(installer)) {
                if (App.Instance.IsValueCreated) {
                    // fail and forget.
                    Fail(IDS_UNABLE_TO_LOCATE_INSTALLER_UI, "Unable to find the CoApp Installer Executable.");
                    return;
                } else {
                    // we never showed the UI yet...
                    App.Instance.Value.Activated += (o, e) => {
                        Fail(IDS_UNABLE_TO_LOCATE_INSTALLER_UI, "Unable to find the CoApp Installer Executable.");
                    };
                    App.Instance.Value.Run();
                    return;
                }
            }
            if (!_cancelling) {
                Process.Start(InstallerUiPath, MsiFilename);
            }
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

        private static void SetRegistryValue(string key, string valueName, string value) {
            try {
                var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(key);
                if (registryKey != null) {
                    registryKey.SetValue(valueName, value);
                }
            }
            catch {
            }
        }

        private static int PositionOfFirstCharacterNotIn(string str, char[] characters) {
            var p = 0;
            while (p < str.Length) {
                if (!characters.Contains(str[p])) {
                    return p;
                }
                p++;
            }
            return p;
        }

        private static ulong Version(string path) {
            try {
                var info = FileVersionInfo.GetVersionInfo(path);
                var fv = info.FileVersion;
                if (!string.IsNullOrEmpty(fv)) {
                    fv = fv.Substring(0, PositionOfFirstCharacterNotIn(fv, "0123456789.".ToCharArray()));
                }

                if (string.IsNullOrEmpty(fv)) {
                    return 0;
                }

                var vers = fv.Split('.');
                var major = vers.Length > 0 ? ToInt32(vers[0]) : 0;
                var minor = vers.Length > 1 ? ToInt32(vers[1]) : 0;
                var build = vers.Length > 2 ? ToInt32(vers[2]) : 0;
                var revision = vers.Length > 3 ? ToInt32(vers[3]) : 0;

                return (((UInt64) major) << 48) + (((UInt64) minor) << 32) + (((UInt64) build) << 16) + (UInt64) revision;
            }
            catch {
                return 0;
            }
        }

        internal static string GetString(uint resourceId, string defaultString) {
            return _resources.Value != null ? (_resources.Value.GetString(resourceId) ?? defaultString) : defaultString;
        }

        public static void InstallCoApp() {
            InstallTask = Task.Factory.StartNew(() => {
                try {
                    if (!_cancelling) {
                        // get coapp.toolkit.msi
                        var file = AcquireFile("CoApp.toolkit.msi",
                            (percentDownloaded) => {
                                if (_mainwindow != null) {
                                    Invoke(() => {
                                        if (percentDownloaded > 0) {
                                            _mainwindow.Opacity = 1;
                                            _mainwindow.installationProgress.Value = percentDownloaded/10;
                                        }
                                    });
                                }
                            });

                        if (!ValidFileExists(file)) {
                            // eew. crappy block 
                            while( _mainwindow == null ) {
                                Thread.Sleep(50);
                            }

                            Fail(IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER, "Unable to download the CoApp Installer MSI");
                            return;
                        }

                        // install msi
                        NativeMethods.MsiSetInternalUI(2, IntPtr.Zero);
                        NativeMethods.MsiSetExternalUI(UiHandler, 0x400, IntPtr.Zero);
                        NativeMethods.MsiInstallProduct(file,
                            String.Format(@"TARGETDIR=""{0}\.installed\"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS", _coAppRootFolder.Value));

                        if (_cancelling) {
                            return;
                        }

                        // If we can, run coapp InstallerUi
                        if (IsCoAppInstalled) {
                            RunInstaller();
                            Application.Current.Shutdown();
                        }
                        else {
                            Fail(IDS_SOMETHING_ODD, "Can't install CoApp Service.");
                        }
                    }
                }
                finally {
                    InstallTask = null;
                }
            });
        }

        private static int ToInt32(string str) {
            int i;
            return Int32.TryParse(str, out i) ? i : 0;
        }

        internal static int UiHandler(IntPtr context, int messageType, string message) {
            // var uiFlags = 0x00FFFFFF & messageType;
            switch ((0xFF000000 & (uint) messageType)) {
                case 0x0A000000:
                    if (message.Length >= 2) {
                        var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(ToInt32).ToArray();

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
                                _currentProgress += msg[3]*_progressDirection;
                                break;
                            case 3:
                                //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                break;
                        }
                    }

                    if (_currentTotalTicks > 0) {
                        var newPercent = (_currentProgress*90/_currentTotalTicks)+10;
                        if (_actualPercent < newPercent) {
                            _actualPercent = newPercent;
                            if (_mainwindow != null) {
                                Invoke(() => { _mainwindow.installationProgress.Value = _actualPercent; });
                            }
                        }
                    }
                    break;
            }
            if( _cancelling ) {
                return 2; // IDCANCEL
            }
            return 1;
        }

        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            // stop the download/install...
            if( !_cancelling ) {
                // check first.
                if( new AreYouSure().ShowDialog() != true ) {
                    _cancelling = true; // prevents any other errors/messages.
                    // wait for MSI to clean up ?
                    if( InstallTask != null ) {
                        InstallTask.Wait();
                    }
                    Application.Current.Shutdown();        
                }
            } else {
                 Application.Current.Shutdown();    
            }
        }

        internal delegate int NativeExternalUIHandler(IntPtr context, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);
    }
}