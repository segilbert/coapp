
using System;
using System.Diagnostics;
using System.Windows;
using CoApp.Updater.Model.Interfaces;
using CoApp.Updater.ViewModel;
using GalaSoft.MvvmLight.Threading;

namespace CoApp.Updater.Model
{
    public class RestartService : IRestartService
    {
        public void Restart()
        {
            var nav = ViewModelLocator.NavigationServiceStatic;

            //save the stack -  nav.Stack
            var info = new ProcessStartInfo
                           {UseShellExecute = true, Verb = "runas", FileName = Application.ResourceAssembly.Location};
            //info.Arguments = The filename for the xml file with the stack.
            try
            {
                Process.Start(info);
                DispatcherHelper.CheckBeginInvokeOnUI(() => Application.Current.Shutdown());
            }
            catch (Exception)
            {
                // don't do anything
            }
            
            
        }
    }
}
