/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:Gui.ViewModel"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using System.Diagnostics.CodeAnalysis;
using CoApp.Updater.Design;
using CoApp.Updater.Model;
using CoApp.Updater.Model.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace CoApp.Updater.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// Use the <strong>mvvmlocatorproperty</strong> snippet to add ViewModels
    /// to this locator.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (ViewModelBase.IsInDesignModeStatic)
            {
                SimpleIoc.Default.Register<IPolicyService, DesignPolicyService>();
            }
            else
            {
                SimpleIoc.Default.Register<IPolicyService, PolicyService>();
            }

            SimpleIoc.Default.Register<INavigationService, NavigationService>();

            SimpleIoc.Default.Register<IUpdateService, UpdateService>();

            SimpleIoc.Default.Register<IPackageManagerService, PackageManagerService>();

            SimpleIoc.Default.Register<IRestartService, RestartService>();

            SimpleIoc.Default.Register<MainWindowViewModel>();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainWindowViewModel MainWindowViewModel
        {
            get { return ServiceLocator.Current.GetInstance<MainWindowViewModel>(); }
        }

        public INavigationService NavigationService
        {
            get { return ServiceLocator.Current.GetInstance<INavigationService>(); }
        }

        public static INavigationService NavigationServiceStatic
        {
            get { return ServiceLocator.Current.GetInstance<INavigationService>(); }
        }

        public IUpdateService UpdateService
        {
            get { return ServiceLocator.Current.GetInstance<IUpdateService>(); }
        }

        public IPackageManagerService PackageManagerService
        {
            get { return ServiceLocator.Current.GetInstance<IPackageManagerService>(); }
        }

        public IRestartService RestartService
        {
            get { return ServiceLocator.Current.GetInstance<IRestartService>(); }
        }

        public static IRestartService RestartServiceStatic
        {
            get { return ServiceLocator.Current.GetInstance<IRestartService>(); }
        }


        public IPolicyService PolicyService
        {
            get { return ServiceLocator.Current.GetInstance<IPolicyService>(); }
        }

        public static IPolicyService PolicyServiceStatic
        {
            get { return ServiceLocator.Current.GetInstance<IPolicyService>(); }
        }

        


        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}