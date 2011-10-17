namespace CoApp.Toolkit.UI {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using Engine;
    using Engine.Client;
    using Exceptions;
    using Extensions;
    using Network;

    /// <summary>
    ///   Interaction logic for InstallerMainWindow.xaml
    /// </summary>
    public partial class InstallerMainWindow : Window {
        private readonly PackageManagerMessages _messages;
        private string _canonicalname;
        private bool clickedInstall;
        private string MsiFilename;

        public InstallerMainWindow(string filename) {
            MsiFilename = filename;
            if (!File.Exists(MsiFilename)) {
                // throw new FileNotFoundException()
                // maybe show a message? 
            }


            InitializeComponent();

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

        private void win_Loaded(object sender, RoutedEventArgs e) {
#if DEBUG
            //this.Hide(); // wpf window can't be hidden by user
#endif
            Task task;
            // try to connect
            task = PackageManager.Instance.Connect("InstallerUI", DateTime.Now.Ticks.ToString());
            task.ContinueWith(
                (t) => {
                    if (t.IsFaulted) {
                        Error(t.Exception);
                    }
                });

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
        }

        private void Error(Exception e) {
            MessageBox.Show("Exception: " + e.Message + "\nInner:" + e.InnerException + "\nStackTrace: " + e.StackTrace);
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

        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            // stop the download/install...
            ((Storyboard)FindResource("hideWindow")).Completed += (ss, ee) => {
                Invoke(
                    () => {
                        Close();
                    });
            };
            ((Storyboard)FindResource("hideWindow")).Begin();
            //Application.Current.Shutdown();
        }

        private void InstallButtonClick(object sender, RoutedEventArgs e) {
            if (!clickedInstall) {
                clickedInstall = true;

                InstallationProgress.Visibility = Visibility.Visible;
                ((Storyboard)FindResource("showProgress")).Begin();
                ((Storyboard)FindResource("hideCheckbox")).Completed += (s, ev) => {
                    UpgradeToLatestVersion.Visibility = Visibility.Hidden;
                };
                ((Storyboard)FindResource("hideCheckbox")).Begin();

                ((Storyboard)FindResource("hideInstall")).Completed += (s, ev) => {
                    InstallButton.Visibility = Visibility.Hidden;
                };
                ((Storyboard)FindResource("hideInstall")).Begin();
                ((Storyboard)FindResource("slideTrans")).Begin();

                PackageManager.Instance.InstallPackage(
                    _canonicalname, autoUpgrade: UpgradeToLatestVersion.IsChecked, messages: new PackageManagerMessages {
                        InstallingPackageProgress = (name, progress, total) => {
                            Invoke(
                                new Action(
                                    () => {
                                        InstallationProgress.Value = total;
                                    }));
                        },
                        InstalledPackage = (pkgName) => {
                            string s = pkgName;
                        },
                        UnexpectedFailure = (anExeption) => {
                            Invoke(
                                () => {
                                    Error(anExeption);
                                });
                        }, // failure
                        Error = (name, argument, reason) => {
                            Invoke(
                                () => {
                                    MessageBox.Show("Error: " + argument + " because of " + reason, name);
                                });
                        },
                        // failure
                        FailedPackageInstall =
                            (name, argument, reason) => {
                                Invoke(
                                    () => {
                                        MessageBox.Show("FailedPackageInstall:\n" + argument + " because of \n" + reason, name);
                                    });
                            } // failure
                    }.Extend(_messages));
                // .ContinueWith((antecedent) => { /Invoke(() => { CloseBtnClick(null, null); }); });
            }
        }

        protected void Invoke(Action action) {
            Dispatcher.Invoke(action);
        }
    }
}