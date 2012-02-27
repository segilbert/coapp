using System.Windows;
using CoApp.Toolkit.Engine.Client;
using CoApp.Updater.ViewModel;
using GalaSoft.MvvmLight.Threading;

namespace CoApp.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
            //PackageManager.Instance.ConnectAndWait("gui-client", null, 5000);
        }

        private void Application_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            PackageManager.Instance.Disconnect();
        }

       
    }
}
