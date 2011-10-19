using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Bootstrapper {
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Win32;

    internal class SingleStep {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        internal delegate int NativeExternalUIHandler(IntPtr context, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);

        private static readonly Lazy<string> BootstrapServerUrl = new Lazy<string>(() => GetRegistryValue(@"Software\CoApp", "BootstrapServerUrl"));
        private const string CoAppUrl = "http://coapp.org/resources/";
        internal static string MsiFilename;
        internal static string MsiFolder;
        internal static string BootstrapFolder;
        private static int _progressDirection = 1;
        private static int _currentTotalTicks = -1;
        private static int _currentProgress;
        internal static bool Cancelling;
        internal static Task InstallTask;

        private static string ExeName {
            get {
                var target = Assembly.GetEntryAssembly().Location;
                if (target.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) {
                    return target;
                }
                // come up with a better EXE name that will work with ShellExecute
                var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                target = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(String.IsNullOrEmpty(fvi.FileName)
                    ? (String.IsNullOrEmpty(fvi.ProductName) ? Path.GetFileName(target) : fvi.ProductName) : fvi.FileName) + ".exe");
                File.Copy(Assembly.GetEntryAssembly().Location, target);
                return target;
            }
        }

        internal static void ElevateSelf(string[] args) {
            try {
                // I'm too lazy to check for admin priviliges, lets let the OS figure it out.
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"Software\CoApp\temp").SetValue("", DateTime.Now.Ticks);
            } catch {
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
                } catch {
                    MainWindow.Fail(LocalizedMessage.IDS_REQUIRES_ADMIN_RIGHTS, "The installer requires administrator permissions.");
                }
            }
        }

        internal static bool IsCoAppInstalled {
            get {
                try {
                    AssemblyCache.QueryAssemblyInfo("CoApp.Toolkit.Engine.Client");
                    return true;
                } catch { }
                return false;
            }
        }

        public static int ActualPercent {
            get { return _actualPercent; }
            set {
                _actualPercent = value;
                if (MainWindow.MainWin != null) {
                    MainWindow.MainWin.Updated();
                }
            }
        }

        private static string GetRegistryValue(string key, string valueName) {
            try {
                var openSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(key);
                if (openSubKey != null) {
                    return openSubKey.GetValue(valueName).ToString();
                }
            } catch {
            }
            return null;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        internal static void RunInstaller(int path) {
            OutputDebugString("Running CoApp :"+path);
            lock (typeof(SingleStep)) {
                try {
                    if (MainWindow.MainWin != null) {
                        MainWindow.MainWin.Visibility = Visibility.Hidden;
                    }
                    
#if DEBUG
                var localAssembly = AcquireFile("CoApp.Toolkit.Engine.Client.dll");
                Debug.WriteLine("Local Assembly: " + localAssembly);
                if (!string.IsNullOrEmpty(localAssembly)) {
                    // use the one found locally.
                    AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceFromAndUnwrap( localAssembly, "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] {MsiFilename}, null, null);
                    // if it didn't throw here, we can assume that the CoApp service is running, and we can get to our assembly.
                    OutputDebugString("Done Creating (local) Appdomain");
                    OutputDebugString("Exiting!");
                    Environment.Exit(0);
                }
#endif

                    OutputDebugString("Creating Domain:" + path);
                    // use strong named assembly
                    AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceAndUnwrap(
                        "CoApp.Toolkit.Engine.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=820d50196d4e8857",
                        "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] { MsiFilename }, null, null);

                    // since we've done everything we need to do, we're out of here. Right Now.
                    OutputDebugString("Exiting:"+path);

                    if( Application.Current != null ) {
                        Application.Current.Shutdown(0);
                    }
                    Environment.Exit(0);
                } catch (Exception e) {
                    OutputDebugString("Failed:" + path);
                }
            }
        }

        internal static string AcquireFile(string filename, Action<int> progressCompleted = null) {
            OutputDebugString("Trying to Acquire:" + filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            var lcid = CultureInfo.CurrentCulture.LCID;
            var localizedName = String.Format("{0}.{1}{2}", name, lcid, extension);
            string f;

            // is the localized file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, localizedName);
                OutputDebugString("   (in Bootstrap folder?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }

            // is the localized file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, localizedName);
                OutputDebugString("   (in Msi folder?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }
            // try the MSI for the localized file 
            f = GetFileFromMSI(localizedName);
            OutputDebugString("   (in Msi?):" + f);
            if (ValidFileExists(f)) {
                OutputDebugString("   Yes.");
                return f;
            }

            //------------------------
            // NORMAL FILE, ON BOX
            //------------------------

            // is the standard file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, filename);
                OutputDebugString("   (in Bootstrap folder?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }

            // is the standard file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, filename);
                OutputDebugString("   (in Msi folder?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }
            // try the MSI for the regular file 
            f = GetFileFromMSI(filename);
            OutputDebugString("   (in MSI?):" + f);
            if (ValidFileExists(f)) {
                OutputDebugString("   Yes.");
                return f;
            }

            //------------------------
            // LOCALIZED FILE, REMOTE
            //------------------------

            // try localized file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = Download(BootstrapServerUrl.Value, localizedName, progressCompleted);
                OutputDebugString("   (on boostrap server?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }

            // try localized file off the coapp server
            f = Download(CoAppUrl, localizedName, progressCompleted);
            OutputDebugString("   (on coapp server?):" + f);
            if (ValidFileExists(f)) {
                OutputDebugString("   Yes.");
                return f;
            }

            // try normal file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = Download(BootstrapServerUrl.Value, filename, progressCompleted);
                OutputDebugString("   (on bootstrap server?):" + f);
                if (ValidFileExists(f)) {
                    OutputDebugString("   Yes.");
                    return f;
                }
            }

            // try normal file off the coapp server
            f = Download(CoAppUrl, filename, progressCompleted);
            OutputDebugString("   (on coapp server?):" + f);
            if (ValidFileExists(f)) {
                OutputDebugString("   Yes.");
                return f;
            }

            OutputDebugString("NOT FOUND:" + filename);
            return null;
        }

        private static string Download(string serverUrl, string filename, Action<int> percentCompleteAction = null) {
            try {
                // the whole URL to the target download
                var uri = new Uri(new Uri(serverUrl), filename);

                OutputDebugString("Trying to download: "+uri.AbsoluteUri);

                // we'll first check to see if the file is there with HEAD 
                // since getting errors back from WebClient is virtually impossible.

                OutputDebugString("TRYING HEAD: " + uri.AbsoluteUri);
                var rq = WebRequest.Create(uri);

                rq.Method = "HEAD";
                rq.GetResponse(); // this will throw on anything other than OK

                OutputDebugString("HEAD OK: " + uri.AbsoluteUri);

                var finished = new ManualResetEvent(false);
                
                var tempFilenme = Path.Combine(Path.GetTempPath(), filename);
                if (File.Exists(tempFilenme)) {
                    File.Delete(tempFilenme);
                }

                var webclient = new WebClient();


                webclient.DownloadProgressChanged += (o, downloadProgressChangedEventArgs) => {
                    if (Cancelling) {
                        webclient.CancelAsync();
                        return;
                    }

                    if (percentCompleteAction != null) {
                        percentCompleteAction(downloadProgressChangedEventArgs.ProgressPercentage);
                    }
                };

                webclient.DownloadFileCompleted += (o, asyncCompletedEventArgs) => {
                    OutputDebugString("Webclient Complete.");

                    try {
                        if (asyncCompletedEventArgs.Cancelled || asyncCompletedEventArgs.Error != null) {
                            if (File.Exists(tempFilenme)) {
                                File.Delete(tempFilenme);
                            }
                        }
                    } finally {
                        finished.Set();    
                    }
                };

                webclient.DownloadFileAsync(uri, tempFilenme, uri.AbsoluteUri + tempFilenme);

                while (!finished.WaitOne(100)) {
                    OutputDebugString("");
                    if (Cancelling) {
                        webclient.CancelAsync();
                        return null;
                    }
                }

                if (File.Exists(tempFilenme)) {
                    return tempFilenme;
                }
            } catch (Exception e) {
                OutputDebugString("(probably normal) Downloading Exception: " + e.Message);
                OutputDebugString(e.StackTrace);
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
                if (bufferSize > 1024 * 1024 * 1024 || bufferSize == 0) {
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
            } finally {
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
                   Debug.WriteLine("   Validity RESULT (assumed): True" );
                   return true;
#else
                    var wtd = new WinTrustData(fileName);
                    var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                    Debug.WriteLine("    RESULT (a): " + (result == WinVerifyTrustResult.Success));
                    return (result == WinVerifyTrustResult.Success);
#endif
                } catch {
                }
            }
            Debug.WriteLine("    RESULT (a): False");
            return false;
        }

        internal static readonly Lazy<string> CoAppRootFolder = new Lazy<string>(() => {
            var result = GetRegistryValue(@"Software\CoApp", "Root");

            if (String.IsNullOrEmpty(result)) {
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


        // we need to keep this around, otherwise the garbage collector gets triggerhappy and cleans up the delegate before the installer is done.
        private static SingleStep.NativeExternalUIHandler uihandler;
        private static int _actualPercent;

        internal static void InstallCoApp() {
            InstallTask = Task.Factory.StartNew(() => {
                try {
                    //OutputDebugString("Started Installer");
                    NativeMethods.MsiSetInternalUI(2, IntPtr.Zero);
                    NativeMethods.MsiSetExternalUI((context, messageType, message) => 1, 0x400, IntPtr.Zero);

                    if (!Cancelling) {
                        var file = MsiFilename;

                        // if this is the CoApp MSI, we don't need to fetch the CoApp MSI.
                        if (!IsCoAppToolkitMSI(MsiFilename)) {
                            // get coapp.toolkit.msi
                            file = AcquireFile("CoApp.Toolkit.msi", percentDownloaded => ActualPercent = percentDownloaded / 10);

                            if (!IsCoAppToolkitMSI(file)) {
                                MainWindow.Fail(LocalizedMessage.IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER, "Unable to download the CoApp Installer MSI");
                                return;
                            }
                        }

                        // We made it past downloading.
                        ActualPercent = 10;

                        // bail if someone has told us to. (good luck!)
                        if (Cancelling) {
                            return;
                        }

                        // get a reference to the delegate. 
                        uihandler = UiHandler;
                        NativeMethods.MsiSetExternalUI(uihandler, 0x400, IntPtr.Zero);

                        OutputDebugString("Running MSI");
                        // install CoApp.Toolkit msi. Don't blink, this can happen FAST!
                        var result = NativeMethods.MsiInstallProduct(file, String.Format(@"TARGETDIR=""{0}\.installed\"" ALLUSERS=1 COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS", CoAppRootFolder.Value));
                        ActualPercent = 100;  // if the UI has not shown, it will try short circuit in the window constructor.

                        // set the ui hander back to nothing.
                        NativeMethods.MsiSetExternalUI(null, 0x400, IntPtr.Zero);
                        OutputDebugString("Done Installing MSI.");

                        // did we succeed?
                        if (result == 0) {
                            // bail if someone has told us to. (good luck!)
                            if (Cancelling) {
                                return;
                            }

                            if (MainWindow.MainWin != null) {
                                MainWindow.MainWin.Dispatcher.BeginInvoke((Action)delegate { RunInstaller(4); });
                            }

                            // if mainwin *is* null, then it's still starting up, and we gotta let it figure out that it's suppose to start the installer.
                            return;
                        }

                        MainWindow.Fail(LocalizedMessage.IDS_SOMETHING_ODD, "Can't install CoApp Service.");
                    }
                } finally {
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
                        _currentProgress += msg[3] * _progressDirection;
                        break;
                    case 3:
                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                        break;
                }
            }

            if (_currentTotalTicks > 0) {
                var newPercent = (_currentProgress * 90 / _currentTotalTicks) + 10;
                if (ActualPercent < newPercent) {
                    ActualPercent = newPercent;
                }
            }

            // if the cancel flag is set, tell MSI
            return Cancelling ? 2 : 1;
        }

        internal static bool IsCoAppToolkitMSI(string filename) {
            if (!ValidFileExists(filename)) {
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

                    if (sb.ToString().StartsWith("CoApp.Toolkit-")) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

