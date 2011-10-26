namespace CoApp.Toolkit.UI {
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using Engine.Client;

    /// <summary>
    ///   Interaction logic for InstallerMainWindow.xaml
    /// </summary>
    public partial class InstallerMainWindow : Window {
        private bool _actionTaken;
        public Installer Installer;

        public InstallerMainWindow(Installer installer) {
            Opacity = 0;
            Installer = installer;
            InitializeComponent();

            OrganizationName.SetBinding(TextBlock.TextProperty, new Binding("Organization") { Source = Installer });
            ProductName.SetBinding(TextBlock.TextProperty, new Binding("Product") { Source = Installer });
            PackageIcon.SetBinding(Image.SourceProperty, new Binding("PackageIcon") { Source = Installer });
            DescriptionText.SetBinding(TextBlock.TextProperty, new Binding("Description") { Source = Installer });
            UpgradeToLatestVersion.SetBinding(ToggleButton.IsCheckedProperty, new Binding("AutomaticallyUpgrade") { Source = Installer });
            ProductVersion.SetBinding(TextBlock.TextProperty, new Binding("ProductVersion") { Source = Installer });
            InstallButton.SetBinding(UIElement.IsEnabledProperty, new Binding("ReadyToInstall") { Source = Installer });
            InstallButton.SetBinding(FrameworkElement.ToolTipProperty , new Binding("InstallButtonText") { Source = Installer });
            InstallText.SetBinding(TextBlock.TextProperty, new Binding("InstallButtonText") { Source = Installer });
            RemoveButton.SetBinding(Button.VisibilityProperty, new Binding("RemoveButtonVisibility") { Source = Installer });
            InstallationProgress.SetBinding(ProgressBar.ValueProperty, new Binding("Progress") { Source = Installer });
            CancelButton.SetBinding(Button.VisibilityProperty, new Binding("CancelButtonVisibility") { Source = Installer });

            try {
                VisibilityAnimation.SetAnimationType(RemoveButton, VisibilityAnimation.AnimationType.Fade);
                VisibilityAnimation.SetAnimationType(InstallButton, VisibilityAnimation.AnimationType.Fade);
                VisibilityAnimation.SetAnimationType(InstallationProgress, VisibilityAnimation.AnimationType.Fade);
                VisibilityAnimation.SetAnimationType(UpgradeToLatestVersion, VisibilityAnimation.AnimationType.Fade);
                VisibilityAnimation.SetAnimationType(CancelButton, VisibilityAnimation.AnimationType.Fade);
            } catch {
                
            }
            Loaded += (src, evnt) => {
                if (!(Opacity > 0) && Installer.ReadyToDisplay) {
                    ((Storyboard)FindResource("showWindow")).Begin();
                }
            };

            Installer.Ready += (src, evnt) => Invoke(() => {
               if (!(Opacity > 0)) {
                   ((Storyboard)FindResource("showWindow")).Begin();
               }
           });

            Installer.Finished += (src, evnt) => Invoke(() => {
                ((Storyboard)FindResource("hideWindow")).Completed += (ss, ee) => { Invoke(Close); };
                ((Storyboard)FindResource("hideWindow")).Begin(); 
            });

        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            
        }


        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            Installer.CancelRequested = true;
        }

        private void InstallButtonClick(object sender, RoutedEventArgs e) {
            if (!_actionTaken) {
                TakeAction();
                Installer.Install();
            }
        }

        protected void Invoke(Action action) {
            Dispatcher.Invoke(action);
        }

        private void TakeAction() {
            _actionTaken = true;

            InstallationProgress.Visibility = Visibility.Visible;
            UpgradeToLatestVersion.Visibility = Visibility.Hidden;
            InstallButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
            ((Storyboard)FindResource("slideTrans")).Begin();
        }

        private void RemoveButtonClick(object sender, RoutedEventArgs e) {
            if (!_actionTaken) {
                TakeAction();
                Installer.Remove();
            }
        }
    }
}