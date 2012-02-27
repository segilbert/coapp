using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;

namespace CoApp.Updater.ViewModel
{
    public class Product : ViewModelBase
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        private string _summary;

        public string Summary
        {
            get { return _summary; }
            set
            {
                _summary = value;
                RaisePropertyChanged("Summary");
            }
        }

        private DateTime _publishDate;

        public DateTime PublishDate
        {
            get { return _publishDate; }
            set
            {
                _publishDate = value;
                RaisePropertyChanged("PublishDate");
            }
        }

        
    }
}
