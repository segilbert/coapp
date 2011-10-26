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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Extensions;
    using Logging;
    using Network;
    using Toolkit.Exceptions;
    using UI;
    using MessageBox = System.Windows.Forms.MessageBox;

    public class Installer : MarshalByRefObject, INotifyPropertyChanged {
        internal string MsiFilename;
        internal Task InstallTask;
        private PackageManagerMessages _messages;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler Ready;
        public event PropertyChangedEventHandler Finished;
        private bool verbose;
        private PackageManager packageManager;
        private Package _specifiedPackage;
        private Package _upgradedPackage;
        private bool _automaticallyUpgrade = true;
        public bool ReadyToDisplay { get; set; }

        public bool AutomaticallyUpgrade {
            get { return _automaticallyUpgrade; }
            set {
                _automaticallyUpgrade = value;
                OnPropertyChanged();
                ProbeForNewerPackageInfo();
            }
        }

        public Package UpgradedPackage {
            get { return _upgradedPackage; }
            set {
                _upgradedPackage = value;
                if (AutomaticallyUpgrade) {
                    OnPropertyChanged();
                }
                OnPropertyChanged("UpgradedPackage");
            }
        }

        public Package SpecifiedPackage {
            get { return _specifiedPackage; }
            set {
                _specifiedPackage = value;
                if (!AutomaticallyUpgrade) {
                    OnPropertyChanged();
                }
                OnPropertyChanged("SpecifiedPackage");
            }
        }


        public Package Package { get {
            return AutomaticallyUpgrade ?  UpgradedPackage ?? SpecifiedPackage : SpecifiedPackage;
        } }

        public bool HasPackage { get { return Package != null; }}

        public Installer(string filename)   {
            if (Keyboard.Modifiers == ModifierKeys.Shift) {
                Logger.Errors = true;
                Logger.Messages = true;
                Logger.Warnings = true;
                verbose = true;
            }

            // we'll take it from here...
            try {
                // First, see if the CoApp Service is running. If it's not, let's get it up and running.
                // this will throw an exception if it's just not possible.
#if DEBUG
                EngineServiceManager.EnsureServiceIsResponding(true);
#else
                  EngineServiceManager.EnsureServiceIsResponding();
#endif
                
                MsiFilename = filename;
                InstallTask = Task.Factory.StartNew(StartInstall);
                
                // if we got this far, CoApp must be running. 
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
                var window = new InstallerMainWindow(this);
                window.ShowDialog();

                if (Application.Current != null) {
                    Application.Current.Shutdown(0);
                }
            } catch (Exception e) {
                MessageBox.Show(e.StackTrace, e.Message);
            }
        }


        /// <summary>
        /// Main install process 
        /// </summary>
        internal void StartInstall() {
            InitMessageHandlers();

            ConnectToPackageManager();

            LoadPackageDetails();
        }

        private void ProbeForNewerPackageInfo() {
            if (AutomaticallyUpgrade) {
                packageManager.GetPackages(SpecifiedPackage.Name + "-*-" +SpecifiedPackage.Architecture + "-"+SpecifiedPackage.PublicKeyToken , latest: true, forceScan: true, messages: _messages).ContinueWith((antecedent) => {
                    if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty()) {
                        UpgradedPackage = null;
                        // DOERROR
                        OnReady();
                        return;
                    }

                    packageManager.GetPackageDetails(antecedent.Result.FirstOrDefault().CanonicalName, _messages).ContinueWith((antecedent2) => {
                        if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty()) {
                            OnReady();
                            // DOERROR
                            return;
                        }

                        try {
                            UpgradedPackage = antecedent.Result.FirstOrDefault();
                            ExtractTrickyPackageInfo();
                        } catch {

                        }
                        OnPropertyChanged();
                        OnReady();
                    }, TaskContinuationOptions.AttachedToParent);
                }, TaskContinuationOptions.AttachedToParent);
            }
        }

        private void LoadPackageDetails() {
            packageManager.GetPackages(Path.GetFullPath(MsiFilename), latest: false, messages: _messages).ContinueWith((antecedent) => {
                if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty() ) {
                    // DOERROR
                    OnReady();
                    return;
                }

                packageManager.GetPackageDetails(antecedent.Result.FirstOrDefault().CanonicalName, _messages).ContinueWith((antecedent2) => {
                    if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty()) {
                        OnReady();
                        // DOERROR
                        return;
                    }

                    try {
                        SpecifiedPackage = antecedent.Result.FirstOrDefault();
                        ExtractTrickyPackageInfo();
                    } catch {
                        
                    }
                    ProbeForNewerPackageInfo();
                    OnPropertyChanged();
                    OnReady();
                }, TaskContinuationOptions.AttachedToParent);
    
            }, TaskContinuationOptions.AttachedToParent);
        }

        private void OnReady() {
            ReadyToDisplay = true;
            if (Ready != null) {
                Ready(this, new PropertyChangedEventArgs("package"));
            }
        }

        private void OnFinished() {
            IsWorking = false;
            if (Finished != null) {
                Finished(this, new PropertyChangedEventArgs("Finished"));
            }
        }

        private void ConnectToPackageManager() {
            packageManager = PackageManager.Instance;
            var t = PackageManager.Instance.Connect("PackageInstaller");
            if (verbose) {
                t.ContinueWith(antecedent => { packageManager.SetLogging(true, true, true); });
            }
        }

        private void InitMessageHandlers() {
            _messages = new PackageManagerMessages {
                UnexpectedFailure = UnexpectedFailure,
                NoPackagesFound = NoPackagesFound,
                PermissionRequired = OperationRequiresPermission,
                Error = MessageArgumentError,
                RequireRemoteFile =
                    (canonicalName, remoteLocations, localFolder, force) =>
                        Downloader.GetRemoteFile(
                            canonicalName, remoteLocations, localFolder, force, new RemoteFileMessages {
                                Progress = (itemUri, percent) => {
                                    "Downloading {0}".format(itemUri.AbsoluteUri).PrintProgressBar(percent);
                                },
                            }, _messages),
                OperationCancelled = CancellationRequested,
                PackageSatisfiedBy = (original, satisfiedBy) => {
                    original.SatisfiedBy = satisfiedBy;
                },
                PackageBlocked = BlockedPackage,
                UnknownPackage = UnknownPackage,
            };
        }

        private void ExtractTrickyPackageInfo() {
            try {
                var image = new BitmapImage();
                // Property changes outside of Begin/EndInit are ignored
                image.BeginInit();
                var srcStream = new MemoryStream(Convert.FromBase64String(Package.Icon));
                image.StreamSource = srcStream;
                image.EndInit();
                image.Freeze();
                _packageIcon = image;
            } catch {
                // didn't take?
            }
        }

        private void Error(Exception e) {
            System.Windows.MessageBox.Show("Exception: " + e.Message + "\nInner:" + e.InnerException + "\nStackTrace: " + e.StackTrace);
            throw e;
        }

        private void UnknownPackage(string canonicalName) {
            Logger.Message("Unknown Package {0}", canonicalName);
        }

        private void BlockedPackage(string canonicalName) {
            Logger.Message("Package {0} is blocked", canonicalName);
        }

        private void CancellationRequested(string obj) {
            Logger.Message("Cancellation Requested.");
        }

        private void MessageArgumentError(string arg1, string arg2, string arg3) {
            Logger.Message("Message Argument Error {0}, {1}, {2}.", arg1, arg2, arg3);
        }

        private void OperationRequiresPermission(string policyName) {
            Logger.Message("Operation requires permission Policy:{0}", policyName);
        }

        private void NoPackagesFound() {
            Logger.Message("Did not find any packages.");
        }

        private void UnexpectedFailure(Exception obj) {
            Logger.Error(obj);
            Error(new ConsoleException("SERVER EXCEPTION: {0}\r\n{1}", obj.Message, obj.StackTrace));
        }

        private BitmapImage _packageIcon;
        public BitmapImage PackageIcon { get { return _packageIcon; }
            set {
                _packageIcon = value;
                OnPropertyChanged("PackageIcon");
            }
        }

        public bool ReadyToInstall {
            get { return HasPackage && (AutomaticallyUpgrade ? UpgradedPackage != null && !UpgradedPackage.IsInstalled : !Package.IsInstalled); }
        }
        
        public bool CanUpgrade {
            get {
                return _specifiedPackage != null && (_specifiedPackage.IsInstalled && (_upgradedPackage != null && !_upgradedPackage.IsInstalled));
            }
        }

        private int _progress;
        public int Progress {
            get { return _progress; } set { _progress = value; OnPropertyChanged("Progress");}
        }

        public Visibility RemoveButtonVisibility { get { return HasPackage && Package.IsInstalled ? Visibility.Visible : Visibility.Hidden; }}
        public Visibility CancelButtonVisibility { get { return CancelRequested ? Visibility.Hidden : Visibility.Visible; } }

        public string InstallButtonText {
            get {
                if( _specifiedPackage != null ) {
                    if (_specifiedPackage.IsInstalled) {
                        if( _upgradedPackage != null ) {
                            if (!_upgradedPackage.IsInstalled) {
                                return "Upgrade";
                            }
                        }
                    }
                    
                }
                return "Install";
            }
        }

        public bool IsInstalled { get { return HasPackage && Package.IsInstalled; }}
        public string Organization { get { return HasPackage ? Package.PublisherName : string.Empty; } }
        public string Description { get { return HasPackage ? Package.Description: string.Empty; } }

        public string Product {
            get {
                return HasPackage ? "{0} - {1}".format(Package.DisplayName, string.IsNullOrEmpty(Package.AuthorVersion) ? Package.Version : Package.AuthorVersion) : string.Empty;
            }
        }

        public string ProductVersion {
            get {
                return HasPackage ? Package.Version : string.Empty;
            }
        }

        private bool _working;

        public bool IsWorking {
            get { return _working; }
            set {
                _working = value;
                OnPropertyChanged("Working");
            }
        }

        private bool _cancel;

        public bool CancelRequested {
            get { return _cancel; }
            set {
                _cancel = value;
                OnPropertyChanged("CancelRequested");
                OnPropertyChanged("CancelButtonVisibility");

                if( !IsWorking ) {
                    OnFinished();
                }
            }
        }

        public void Install() {
            if( !IsWorking) {
                IsWorking = true;
                packageManager.InstallPackage(Package.CanonicalName, autoUpgrade: false, messages: new PackageManagerMessages {
                    InstallingPackageProgress = (canonicalName, progress, overallProgress) => { Progress = overallProgress; },
                }.Extend(_messages)).ContinueWith(antecedent => OnFinished(), TaskContinuationOptions.AttachedToParent);
            }
        }

        public void Remove() {
            if (!IsWorking) {
                IsWorking = true;
                packageManager.RemovePackage(Package.CanonicalName, messages: new PackageManagerMessages {
                    RemovingPackageProgress= (canonicalName, progress) => {
                        Progress = progress;
                    },
                }).ContinueWith(antecedent => {
                    OnFinished();
                }, TaskContinuationOptions.AttachedToParent);
            }
        }

        protected void OnPropertyChanged(string name=null) {
            if (PropertyChanged != null) {
                if (name == null) {
                    foreach (var propertyName in GetType().GetProperties().Select(each => each.Name)) {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                } else {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}