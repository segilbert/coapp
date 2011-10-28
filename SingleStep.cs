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
    using System.Windows.Input;
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

        [STAThreadAttribute]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(string[] args) {
            if (Keyboard.Modifiers == ModifierKeys.Shift) {
                Logger.Errors = true;
                Logger.Messages = true;
                Logger.Warnings = true;
            }

            var commandline = args.Aggregate(string.Empty, (current, each) => current + " " + each).Trim();

            Logger.Warning("Startup :" + commandline);
            // Ensure that we are elevated. If the app returns from here, we are.
            ElevateSelf(commandline);

            // get the folder of the bootstrap EXE
            BootstrapFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
            if (!Cancelling) {
                if (commandline.Length == 0) {
                    MainWindow.Fail(LocalizedMessage.IDS_MISSING_MSI_FILE_ON_COMMANDLINE, "Missing MSI package name on command line!");
                } else if (!File.Exists(Path.GetFullPath(commandline))) {
                    MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_FOUND, "Specified MSI package name does not exist!");
                } else if (!ValidFileExists(Path.GetFullPath(commandline))) {
                    MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_VALID, "Specified MSI package is not signed with a valid certificate!");
                } else {
                    // have a valid MSI file. Alrighty!
                    MsiFilename = Path.GetFullPath(commandline);
                    MsiFolder = Path.GetDirectoryName(MsiFilename);

                    // if this installer is present, this will exit right after.
                    if (IsCoAppInstalled) {
                        RunInstaller(1);
                        return;
                    }

                    // if CoApp isn't there, we gotta get it.
                    // this is a quick call, since it spins off a task in the background.
                    InstallCoApp();
                }
            }
            // start showin' the GUI.
            // Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            new Application {
                StartupUri = new Uri("MainWindow.xaml", UriKind.Relative)
            }.Run();
        }

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

        internal static void ElevateSelf(string args) {
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
                            Arguments = args,
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

        internal static void RunInstaller(int path) {
            Logger.Warning("Running CoApp :" + path);
            lock (typeof(SingleStep)) {
                try {
                    if (MainWindow.MainWin != null) {
                        MainWindow.MainWin.Visibility = Visibility.Hidden;
                    }
                    
#if DEBUG
                var localAssembly = AcquireFile("CoApp.Toolkit.Engine.Client.dll");
                Logger.Message("Local Assembly: " + localAssembly);
                if (!string.IsNullOrEmpty(localAssembly)) {
                    // use the one found locally.
                    AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceFromAndUnwrap( localAssembly, "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] {MsiFilename}, null, null);
                    // if it didn't throw here, we can assume that the CoApp service is running, and we can get to our assembly.
                    Logger.Warning("Done Creating (local) Appdomain");
                    Logger.Warning("Exiting!");
                    Environment.Exit(0);
                }
#endif

                Logger.Warning("Creating Domain:" + path);
                    // use strong named assembly
                    AppDomain.CreateDomain("tmp" + DateTime.Now.Ticks).CreateInstanceAndUnwrap(
                        "CoApp.Toolkit.Engine.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=820d50196d4e8857",
                        "CoApp.Toolkit.Engine.Client.Installer", false, BindingFlags.Default, null, new[] { MsiFilename }, null, null);

                    // since we've done everything we need to do, we're out of here. Right Now.
                    Logger.Warning("Exiting:" + path);

                    if( Application.Current != null ) {
                        Application.Current.Shutdown(0);
                    }
                    Environment.Exit(0);
                } catch (Exception e) {
                    Logger.Warning(e);
                }
            }
        }

        internal static string AcquireFile(string filename, Action<int> progressCompleted = null) {
            Logger.Warning("Trying to Acquire:" + filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            var lcid = CultureInfo.CurrentCulture.LCID;
            var localizedName = String.Format("{0}.{1}{2}", name, lcid, extension);
            string f;

            // is the localized file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, localizedName);
                Logger.Warning("   (in Bootstrap folder?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }

            // is the localized file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, localizedName);
                Logger.Warning("   (in Msi folder?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }
            // try the MSI for the localized file 
            f = GetFileFromMSI(localizedName);
            Logger.Warning("   (in Msi?):" + f);
            if (ValidFileExists(f)) {
                Logger.Warning("   Yes.");
                return f;
            }

            //------------------------
            // NORMAL FILE, ON BOX
            //------------------------

            // is the standard file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, filename);
                Logger.Warning("   (in Bootstrap folder?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }

            // is the standard file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, filename);
                Logger.Warning("   (in Msi folder?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }
            // try the MSI for the regular file 
            f = GetFileFromMSI(filename);
            Logger.Warning("   (in MSI?):" + f);
            if (ValidFileExists(f)) {
                Logger.Warning("   Yes.");
                return f;
            }

            //------------------------
            // LOCALIZED FILE, REMOTE
            //------------------------

            // try localized file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = AsyncDownloader.Download(BootstrapServerUrl.Value, localizedName, progressCompleted);
                Logger.Warning("   (on boostrap server?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }

            // try localized file off the coapp server
            f = AsyncDownloader.Download(CoAppUrl, localizedName, progressCompleted);
            Logger.Warning("   (on coapp server?):" + f);
            if (ValidFileExists(f)) {
                Logger.Warning("   Yes.");
                return f;
            }

            // try normal file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = AsyncDownloader.Download(BootstrapServerUrl.Value, filename, progressCompleted);
                Logger.Warning("   (on bootstrap server?):" + f);
                if (ValidFileExists(f)) {
                    Logger.Warning("   Yes.");
                    return f;
                }
            }

            // try normal file off the coapp server
            f = AsyncDownloader.Download(CoAppUrl, filename, progressCompleted);
            Logger.Warning("   (on coapp server?):" + f);
            if (ValidFileExists(f)) {
                Logger.Warning("   Yes.");
                return f;
            }

            Logger.Warning("NOT FOUND:" + filename);
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
            Logger.Message("Checking for file: " + fileName);
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName)) {
                try {
#if DEBUG
                   Logger.Message("   Validity RESULT (assumed): True");
                   return true;
#else
                    var wtd = new WinTrustData(fileName);
                    var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                    Logger.Message("    RESULT (a): " + (result == WinVerifyTrustResult.Success));
                    return (result == WinVerifyTrustResult.Success);
#endif
                } catch {
                }
            }
            Logger.Message("    RESULT (a): False");
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

                    Logger.Warning("Started Toolkit Installer");
                    Thread.Sleep(4000);
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

                        Logger.Warning("Running MSI");
                        // install CoApp.Toolkit msi. Don't blink, this can happen FAST!
                        var result = NativeMethods.MsiInstallProduct(file, String.Format(@"TARGETDIR=""{0}\.installed\"" ALLUSERS=1 COAPP_INSTALLED=1 REBOOT=REALLYSUPPRESS", CoAppRootFolder.Value));
                        ActualPercent = 100;  // if the UI has not shown, it will try short circuit in the window constructor.

                        try {
                            var CoAppCacheFolder = Path.Combine(CoAppRootFolder.Value, ".cache");
                            Directory.CreateDirectory(CoAppCacheFolder);

                            var cachedPath = Path.Combine(CoAppCacheFolder, MsiCanonicalName + ".msi");
                            if (!File.Exists(cachedPath)) {
                                File.Copy(file, cachedPath);
                            }
                        } catch(Exception e) {
                            Logger.Error(e);
                        }

                        // set the ui hander back to nothing.
                        NativeMethods.MsiSetExternalUI(null, 0x400, IntPtr.Zero);
                        Logger.Warning("Done Installing MSI.");

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


        private static string MsiCanonicalName;
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
                    
                    size = 1024;
                    var sb2 = new StringBuilder(1024);
                    NativeMethods.MsiGetProperty(hProduct, "CanonicalName ", sb2, ref size);
                    NativeMethods.MsiCloseHandle(hProduct);

                    if (sb.ToString().Equals("CoApp.Toolkit")) {
                        MsiCanonicalName = sb2.ToString();
                        return true;
                    }
                }
            }
            return false;
        }
    }

 
}

