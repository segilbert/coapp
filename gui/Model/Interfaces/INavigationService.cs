using System.Collections.ObjectModel;
using CoApp.Updater.ViewModel;
using GalaSoft.MvvmLight;

namespace CoApp.Updater.Model.Interfaces
{
    
    public interface INavigationService
    {

        void GoTo(ScreenViewModel viewModel);
        void Back();


        ReadOnlyCollection<ScreenViewModel> Stack { get; }

        
        bool StackEmpty { get; }
    }
}