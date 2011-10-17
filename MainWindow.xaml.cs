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
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Win32;

    internal enum LocalizedMessage {
        IDS_ERROR_CANT_OPEN_PACKAGE = 500,
        IDS_MISSING_MSI_FILE_ON_COMMANDLINE,
        IDS_REQUIRES_ADMIN_RIGHTS,
        IDS_SOMETHING_ODD,
        IDS_FRAMEWORK_INSTALL_CANCELLED,
        IDS_UNABLE_TO_DOWNLOAD_FRAMEWORK,
        IDS_UNABLE_TO_FIND_SECOND_STAGE,
        IDS_MAIN_MESSAGE,
        IDS_CANT_CONTINUE,
        IDS_FOR_ASSISTANCE,
        IDS_OK_TO_CANCEL,
        IDS_CANCEL,
        IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER,
        IDS_UNABLE_TO_LOCATE_INSTALLER_UI,
        IDS_UNABLE_TO_ACQUIRE_RESOURCES,
        IDS_CANCELLING,
        IDS_MSI_FILE_NOT_FOUND,
        IDS_MSI_FILE_NOT_VALID,
    }


    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        internal static string MsiFilename;
        internal static string MsiFolder;
        internal static string BootstrapFolder;
        internal static Task InstallTask;
        internal static bool Cancelling;
        internal static bool ReadyToInstall;
        internal static MainWindow MainWin;

        private const string CoAppUrl = "http://coapp.org/resources/";
        private const string HelpUrl = "http://coapp.org/help/";
        
        private static int _progressDirection = 1;
        private static int _currentTotalTicks = -1;
        private static int _currentProgress;
        private static int _actualPercent = 0;

        private static readonly Lazy<string> BootstrapServerUrl = new Lazy<string>(() => GetRegistryValue(@"Software\CoApp", "BootstrapServerUrl"));
        private static readonly Lazy<NativeResourceModule> NativeResources = new Lazy<NativeResourceModule>(() => {
            try {
                return new NativeResourceModule(AcquireFile("CoApp.Resources.dll"));
            }
            catch {
                return null;
            }
        }, LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<string> CoAppRootFolder = new Lazy<string>(() => {
            var result = GetRegistryValue(@"Software\CoApp", "Root");

            if (string.IsNullOrEmpty(result)) {
                result = String.Format("{0}\\apps", Environment.GetEnvironmentVariable("SystemDrive"));
                try {
                    var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"Software\CoApp");
                    if (registryKey != null) {
                        registryKey.SetValue("Root", result);
                    }
                } catch {
                }
            }

            if (!Directory.Exists(result)) {
                Directory.CreateDirectory(result);
            }

            return result;
        });

        // [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        // public static extern void OutputDebugString(string message);

        public MainWindow() {
            InitializeComponent();
            Opacity = 0;
            if (NativeResources.Value != null) {
                containerPanel.Background.SetValue(ImageBrush.ImageSourceProperty, NativeResources.Value.GetBitmapImage(1201));
                logoImage.SetValue(Image.SourceProperty, NativeResources.Value.GetBitmapImage(1202));
            }
            // try to short circuit early
            if( ReadyToInstall) {
                SetProgress(100); // kill our UI
                TryInstaller();
            }

            Loaded += (o, e) => {
                // try to short circuit before we get far...
                if (ReadyToInstall) {
                    SetProgress(100); // kill our UI
                    TryInstaller();
                }

                MainWin = this;
                if( !Cancelling ) {
                    SetProgress(_actualPercent);
                }
            };
        }

        // this should only be called on the main thread
        internal static void TryInstaller() {
            if (MainWin != null && !MainWin.Dispatcher.CheckAccess()) {
                // this call came in on the wrong thread.
                // reroute it to the GUI thread.
                MainWin.Dispatcher.Invoke((Action)(TryInstaller));
                // but we'll wait for it to end.
                return;
            }
            lock (typeof(MainWindow)) {
            try {
                
                    // we're gonna look around to see if we have a CoApp.Toolkit.Engine.Client.dll first, 'cause if we've got one, we will try that.
                    // this allows us to code + debug without wanting to blow our brains out.

                    var localAssembly = AcquireFile("CoApp.Toolkit.Engine.Client.dll");

                    Debug.WriteLine("Local Assembly: " + localAssembly);

                    if (string.IsNullOrEmpty(localAssembly)) {
                        // use strong named assembly
                        AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceAndUnwrap(
                            "CoApp.Toolkit.Engine.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=820d50196d4e8857",
                            "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] {MsiFilename}, null, null);
                    } else {
                        // use the one found locally.
                        AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceFromAndUnwrap(
                            localAssembly,
                            "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] {MsiFilename}, null, null);
                    }
                    // since we've done everything we need to do, we're out of here. Right Now.
                    Environment.Exit(0);
                
                } catch {
                }
            }
        }

        private static string AcquireFile(string filename, Action<int> progressCompleted = null) {
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
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = Download(BootstrapServerUrl.Value, localizedName,progressCompleted);
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
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = Download(BootstrapServerUrl.Value, filename,progressCompleted);
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

        private static string Download(string serverUrl, string filename, Action<int> percentCompleteAction = null) {
            try {
                // the whole URL to the target download
                var uri = new Uri(new Uri(serverUrl), filename);

                // we'll first check to see if the file is there with HEAD 
                // since getting errors back from WebClient is virtually impossible.

                var rq = WebRequest.Create(uri);
                
                rq.Method = "HEAD";
                rq.GetResponse(); // this will throw on anything other than OK

                var finished = new ManualResetEvent(false);
                
                var tempFilenme = Path.Combine(Path.GetTempPath(), filename);
                if (File.Exists(tempFilenme)) {
                    File.Delete(tempFilenme);
                }

                var webclient = new WebClient();

                webclient.DownloadProgressChanged += (o, downloadProgressChangedEventArgs) => {
                    if( Cancelling ) {
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
                    if( Cancelling) {
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

        internal static bool ValidFileExists(string fileName) {
            Debug.WriteLine("Checking for file: " + fileName);
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName)) {
               try {
#if DEBUG
                   Debug.WriteLine("    RESULT (assumed): True" );
                   return true;
#else
                    var wtd = new WinTrustData(fileName);
                    var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                    Debug.WriteLine("    RESULT (a): " + (result == WinVerifyTrustResult.Success));
                    return (result == WinVerifyTrustResult.Success);
#endif
               }
                catch {
                }
            }
            Debug.WriteLine("    RESULT (a): False" );
            return false;
        }

        internal static void Fail(LocalizedMessage message, string messageText) {
            if (!Cancelling) {
                Task.Factory.StartNew( () => {
                    Cancelling = true;
                    messageText = GetString(message, messageText);

                    while (MainWin == null) {
                        Thread.Sleep(20);
                    }

                    MainWin.Dispatcher.Invoke((Action) delegate {
                        MainWin.containerPanel.Background = new SolidColorBrush(
                            new Color {
                                A = 255,
                                R = 18,
                                G = 112,
                                B = 170
                            });
                                
                        MainWin.progressPanel.Visibility = Visibility.Collapsed;
                        MainWin.failPanel.Visibility = Visibility.Visible;
                        MainWin.messageText.Text = messageText;
                        MainWin.helpLink.NavigateUri = new Uri(HelpUrl + (message + 100));
                        MainWin.helpLink.Inlines.Clear();
                        MainWin.helpLink.Inlines.Add(new Run(HelpUrl + (message + 100)));
                        MainWin.Visibility = Visibility.Visible;
                        MainWin.Opacity = 1;
                    });
                });
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

        internal static string GetString(LocalizedMessage resourceId, string defaultString) {
            return NativeResources.Value != null ? (NativeResources.Value.GetString((uint)resourceId) ?? defaultString) : defaultString;
        }

        private static bool IsCoAppToolkitMSI(string filename ) {
            if( !ValidFileExists(filename)) {
                return false;
            }

            // First, check to see if the msi we've got *is* the coapp.toolkit msi file :)
            var cert = new X509Certificate2(filename);
            // CN=OUTERCURVE FOUNDATION, OU=CoApp Project, OU=Digital ID Class 3 - Microsoft Software Validation v2, O=OUTERCURVE FOUNDATION, L=Redmond, S=Washington, C=US
            if (cert.Subject.StartsWith("CN=OUTERCURVE FOUNDATION") && cert.Subject.Contains("OU=CoApp Project")) {
                int hProduct;
                if (NativeMethods.MsiOpenPackageEx(filename, 1, out hProduct) == 0) {
                    var sb = new StringBuilder(1024);
                    uint size = 1024;
                    NativeMethods.MsiGetProperty(hProduct, "ProductName", sb, ref size);
                    NativeMethods.MsiCloseHandle(hProduct);

                    if (sb.ToString() == "CoApp.Toolkit") {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void SetProgress(int progress) {
            if (MainWin != null) {
                if (MainWin.Dispatcher.CheckAccess()) {
                    if (progress > 0 && progress < 100) {
                        MainWin.Opacity = 1;
                        MainWin.installationProgress.Value = progress;
                    }

                    if (progress >= 100) {
                        MainWin.Visibility = Visibility.Hidden;
                        // hide when we are finished here.
                    }
                } else {
                    // tell it to get on the right thread, but we're not waiting for it.
                    MainWin.Dispatcher.BeginInvoke((Action)(() => SetProgress(progress)));
                }
                Thread.Sleep(20); // give a chance to have the UI update?
            }
        }

        // we need to keep this around, otherwise the garbage collector gets triggerhappy and cleans up the delegate before the installer is done.
        private static NativeExternalUIHandler uihandler;

        internal static void InstallCoApp() {
            InstallTask = Task.Factory.StartNew(() => {
                try {
                    NativeMethods.MsiSetInternalUI(2, IntPtr.Zero);
                    NativeMethods.MsiSetExternalUI((context, messageType, message) => 1, 0x400, IntPtr.Zero);

                    if (!Cancelling) {
                        var file = MsiFilename;

                        // if this is the CoApp MSI, we don't need to fetch the CoApp MSI.
                        if (!IsCoAppToolkitMSI(MsiFilename)) {
                            // get coapp.toolkit.msi
                            file = AcquireFile("CoApp.toolkit.msi", percentDownloaded => SetProgress(percentDownloaded/10));

                            if (!IsCoAppToolkitMSI(file)) {
                                Fail(LocalizedMessage.IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER, "Unable to download the CoApp Installer MSI");
                                return;
                            }
                        }

                        // We made it past downloading.
                        _actualPercent = 10;
                        SetProgress(_actualPercent);

                        // bail if someone has told us to. (good luck!)
                        if (Cancelling) {
                            return;
                        }
                        
                        // get a reference to the delegate. 
                        uihandler = UiHandler;
                        NativeMethods.MsiSetExternalUI(uihandler, 0x400, IntPtr.Zero); 

                        // install CoApp.Toolkit msi. Don't blink, this can happen FAST!
                        var result = NativeMethods.MsiInstallProduct(file, String.Format(@"TARGETDIR=""{0}\.installed\"" COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS", CoAppRootFolder.Value));

                        // set the ui hander back to nothing.
                        NativeMethods.MsiSetExternalUI(null, 0x400, IntPtr.Zero);

                        // did we succeed?
                        if( result == 0 ) {
                            ReadyToInstall = true; // if the UI has not shown, it will try short circuit in the window constructor.

                            _actualPercent = 100;
                            SetProgress(_actualPercent); // hides any UI that might be showing, tells it not to show if it's not shown yet.
                            
                            if( MainWin == null ) {
                                Thread.Sleep(5000); 
                                // if the main window hasn't started up yet, we'll wait for a really long time
                                // before we try to continue.
                                // When it does run, either it will have shortcircuited us, (and hence, the thread will be busy until the installer is done)
                                // or ... somehow we managed to not call the installer, in which case, we'll try it one last time
                                // and it should work. If it doesn't ... well, sometimes life is like that.
                            }

                            TryInstaller(); // if it manages to call it here, good. If not, well, this is a baaaad day.
                        }

                        Fail(LocalizedMessage.IDS_SOMETHING_ODD, "Can't install CoApp Service.");
                    }
                }
                finally {
                    InstallTask = null;
                }
            });
        }

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
                    SetProgress(_actualPercent);
                }
            }
            
            // if the cancel flag is set, tell MSI
            return Cancelling ? 2 : 1;
        }

        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            // stop the download/install...
            if( !Cancelling ) {
                // check first.
                if( new AreYouSure().ShowDialog() != true ) {
                    Cancelling = true; // prevents any other errors/messages.
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