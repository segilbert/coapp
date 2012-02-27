using System.Collections.Generic;
using System.Windows.Input;
using CoApp.Updater.Model;
using CoApp.Updater.Model.Interfaces;
using GalaSoft.MvvmLight.Command;

namespace CoApp.Updater.ViewModel
{
    public class PrimaryViewModel : ScreenViewModel
    {
        internal IUpdateService UpdateService;

        public PrimaryViewModel()
        {
            UpdateService = new ViewModelLocator().UpdateService;
            NumberOfProducts = UpdateService.NumberOfProducts;
            NumberOfProductsSelected = UpdateService.NumberOfProducts;
            RunAdmin = new RelayCommand(() => ViewModelLocator.RestartServiceStatic.Restart());
        }

        private IList<string> _errors;
        private int _numberOfProducts;
        private IList<string> _warnings;

        public int NumberOfProducts
        {
            get { return _numberOfProducts; }
            set
            {
                _numberOfProducts = value;
                RaisePropertyChanged("NumberOfProducts");
            }
        }

        private int _numberOfProductsSelected;

        public int NumberOfProductsSelected
        {
            get { return _numberOfProductsSelected; }
            set
            {
                _numberOfProductsSelected = value;
                RaisePropertyChanged("NumberOfProductsSelected");
            }
        }



        public IEnumerable<string> Warnings
        {
            get { return _warnings; }
            private set
            {
                _warnings = new List<string>(value);
                RaisePropertyChanged("Warnings");
            }
        }


        public IEnumerable<string> Errors
        {
            get { return _errors; }
            private set
            {
                _errors = new List<string>(value);
                RaisePropertyChanged("Errors");
            }
        }

        public ICommand RunAdmin { get; set; }
    


}
}