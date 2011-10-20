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
    using System.Windows.Media.Imaging;
    using Extensions;
    using Network;
    using Toolkit.Exceptions;
    using UI;
    using MessageBox = System.Windows.Forms.MessageBox;

    public class Installer : MarshalByRefObject, INotifyPropertyChanged {
        internal string MsiFilename;
        internal Task InstallTask;
        private PackageManagerMessages _messages;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler PackageUpdated;
        private PackageManager packageManager;
        private Package _package;

        public Installer(string filename)   {
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

            ProbeForNewerPackageInfo();
        }

        private void ProbeForNewerPackageInfo() {
            
        }

        private void LoadPackageDetails() {
            packageManager.GetPackages(Path.GetFullPath(MsiFilename), latest: false, messages: _messages).ContinueWith((antecedent) => {
                if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty() ) {
                    // DOERROR
                    return;
                }

                packageManager.GetPackageDetails(antecedent.Result.FirstOrDefault().CanonicalName, _messages).ContinueWith((antecedent2) => {
                    if (antecedent.IsFaulted || antecedent.Result.IsNullOrEmpty()) {
                        // DOERROR
                        return;
                    }

                    _package = antecedent.Result.FirstOrDefault();
                    ExtractTrickyPackageInfo();
                    OnPropertyChanged();
                    if (PackageUpdated != null) {
                        PackageUpdated(this, new PropertyChangedEventArgs("package"));
                    }
                },
                    TaskContinuationOptions.AttachedToParent);
    
            }, TaskContinuationOptions.AttachedToParent);
        }

        private void ConnectToPackageManager() {
            packageManager = PackageManager.Instance;
            PackageManager.Instance.Connect("PackageInstaller");
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
                var srcStream = new MemoryStream(Convert.FromBase64String(_package.Icon));
                image.StreamSource = srcStream;
                image.EndInit();
                image.Freeze();
                var x = image.PixelWidth;
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
            Debug.WriteLine("Unknown Package {0}", canonicalName);
        }

        private void BlockedPackage(string canonicalName) {
            Debug.WriteLine("Package {0} is blocked", canonicalName);
        }

        private void CancellationRequested(string obj) {
            Debug.WriteLine("Cancellation Requested.");
        }

        private void MessageArgumentError(string arg1, string arg2, string arg3) {
            Debug.WriteLine("Message Argument Error {0}, {1}, {2}.", arg1, arg2, arg3);
        }

        private void OperationRequiresPermission(string policyName) {
            Debug.WriteLine("Operation requires permission Policy:{0}", policyName);
        }

        private void NoPackagesFound() {
            Debug.WriteLine("Did not find any packages.");
        }

        private void UnexpectedFailure(Exception obj) {
            Error(new ConsoleException("SERVER EXCEPTION: {0}\r\n{1}", obj.Message, obj.StackTrace));
        }

        private BitmapImage _packageIcon;
        public BitmapImage PackageIcon { get { return _packageIcon; }
            set {
                _packageIcon = value;
                OnPropertyChanged("PackageIcon");
            }
        }

        public string Organization { get { return _package == null ? string.Empty :  _package.PublisherName; } }
        public string Description { get { return _package == null ? string.Empty : _package.Description; } }


        public string Product {
            get {
                return _package == null
                    ? string.Empty : "{0} - {1}".format(_package.DisplayName, string.IsNullOrEmpty(_package.AuthorVersion) ? _package.Version : _package.AuthorVersion);
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

    /* 
     * #if DEBUG
            //this.Hide(); // wpf window can't be hidden by user
#endif
            Task task;
            // try to connect
            

            // PackageManager.Instance.GetPackages( with path via command line
            if (MsiFilename.EndsWith(".msi") && File.Exists(MsiFilename)) {
                // assume install if the only thing given is a filename.
                // lates = state of the checkbox. messages? rest null.

                task =
                    PackageManager.Instance.GetPackages(Path.GetFullPath(MsiFilename), latest: UpgradeToLatestVersion.IsChecked, messages: _messages).
                        ContinueWith(
                            antecedent => {
                                if (antecedent.Result.Count() > 0) {
                                    var myPackage = antecedent.Result.First();
                                    // fill myPackage with info
                                    task = PackageManager.Instance.GetPackageDetails(myPackage.CanonicalName, messages: _messages).ContinueWith(
                                        (antecedent2) => {
                                            _canonicalname = myPackage.CanonicalName;
                                            Invoke(
                                                () => {
                                                    ProductName.Text = myPackage.DisplayName;
                                                    OrganizationName.Text = myPackage.PublisherName;
                                                    DescriptionText.Text = myPackage.Description;
                                                    var image = new BitmapImage();
                                                    // Property changes outside of Begin/EndInit are ignored
                                                    image.BeginInit();
                                                    var srcStream = new MemoryStream(Convert.FromBase64String(myPackage.Icon));
                                                    image.StreamSource = srcStream;
                                                    image.EndInit();
                                                    PackageIcon.Source = image;
                                                    InstallButton.IsEnabled = true;
                                                });
                                        });
                                } else {
                                    Invoke(
                                        () => {
                                            Error(new Exception("package not found"));
                                        });
                                }
                            });
            } else {
                // todo: proper error message (for wrong arguments)
                Error(new Exception("Something went wrong. Improper arguments on command line"));
            }
     * */
}