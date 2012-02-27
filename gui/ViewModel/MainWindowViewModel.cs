using System;
using System.ComponentModel;
using System.Windows.Input;
using CoApp.Updater.Messages;
using CoApp.Updater.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace CoApp.Updater.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class MainWindowViewModel : ScreenViewModel
    {
       
        

        private ScreenViewModel _mainScreenViewModel;

        public ScreenViewModel MainScreenViewModel
        {
            get { return _mainScreenViewModel; }
            set
            {
                if (_mainScreenViewModel != null)
                {
                    _mainScreenViewModel.PropertyChanged -= MainScreenViewModelOnPropertyChanged;
                    _mainScreenViewModel.FireUnload();
                }
                _mainScreenViewModel = value;
                if (_mainScreenViewModel != null)
                {
                    _mainScreenViewModel.PropertyChanged -= MainScreenViewModelOnPropertyChanged;
                    MainScreenViewModel.FireLoad();
                    ResetTitle();
                    ResetSubTitle();
                    
                }
                RaisePropertyChanged("MainScreenViewModel");
                RaisePropertyChanged("Title");
                RaisePropertyChanged("SubTitle");
                RaisePropertyChanged("DisplayBackButton");
            }
        }
        
        public bool DisplayBackButton
        {
            get { return !ViewModelLocator.NavigationServiceStatic.StackEmpty; }
        }
        
        private void MainScreenViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            //check for title and subtitle
            switch(propertyChangedEventArgs.PropertyName)
            {
                case "Title":
                    ResetTitle();
                    break;
                case "SubTitle":
                    ResetSubTitle();
                    break;
            }
        }

        private void ResetTitle()
        {
            Title = MainScreenViewModel.Title;
        }

        private void ResetSubTitle()
        {
            Title = MainScreenViewModel.Title;
        }


        

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainWindowViewModel()
        {
            Messenger.Default.Register<GoToMessage>(this, ActOnNavigate);
            Back = new RelayCommand(() => ViewModelLocator.NavigationServiceStatic.Back());
            ViewModelLocator.NavigationServiceStatic.GoTo(new UpdatingViewModel());
        }

        private void ActOnNavigate(GoToMessage msg)
        {

			if (IsInDesignMode)
			{
				MainScreenViewModel = msg.Destination;
			}
			else
			{
				DispatcherHelper.CheckBeginInvokeOnUI(() => MainScreenViewModel = msg.Destination);
			}
            
        }
        
        public ICommand Back { get; set; }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}