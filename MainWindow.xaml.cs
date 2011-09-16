namespace CoApp.Bootstrapper {
    using System.Drawing;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using BootstrapperUI;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            sp1.Background.SetValue(ImageBrush.ImageSourceProperty, NativeResources.GetBitmapImage(1201));
            logoImage.SetValue(System.Windows.Controls.Image.SourceProperty, NativeResources.GetBitmapImage(1202));
        }

        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e) {
            // stop the download/install...
            Application.Current.Shutdown();
        }
    }
}
