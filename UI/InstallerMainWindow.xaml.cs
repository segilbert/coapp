namespace CoApp.Toolkit.UI {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
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
        private bool _clickedInstall;
        public Installer Installer;

        public InstallerMainWindow(Installer installer) {
            Opacity = 0;
            Installer = installer;
            InitializeComponent();

            OrganizationName.SetBinding(TextBlock.TextProperty, new Binding("Organization") { Source = Installer });
            ProductName.SetBinding(TextBlock.TextProperty, new Binding("Product") { Source = Installer });
            PackageIcon.SetBinding(Image.SourceProperty, new Binding("PackageIcon") { Source = Installer });
            DescriptionText.SetBinding(TextBlock.TextProperty, new Binding("Description") { Source = Installer });

            // DescriptionBrowser.NavigateToString("This is a test of the text");
            // DescriptionBrowser.Navigate("http://slashdot.org");

            Installer.PackageUpdated += (src, evnt) => Invoke(() => {
               if (Opacity < 1) {
                   ((Storyboard)FindResource("showWindow")).Begin();
               }
               
           });
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            // ((Storyboard)FindResource("showWindow")).Completed += (ss, ee) => { Invoke(Close); };
            //((Storyboard)FindResource("showWindow")).Begin();
        }


        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            // stop the download/install...
            ((Storyboard)FindResource("hideWindow")).Completed += (ss, ee) => {  Invoke(Close); };
            ((Storyboard)FindResource("hideWindow")).Begin();
            //Application.Current.Shutdown();
        }

        private void InstallButtonClick(object sender, RoutedEventArgs e) {
            if (!_clickedInstall) {
                _clickedInstall = true;

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
                /*
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
                 * */
            }
        }

        protected void Invoke(Action action) {
            Dispatcher.Invoke(action);
        }
    }
}