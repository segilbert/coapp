using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoApp.Updater.Model.Interfaces;
using GalaSoft.MvvmLight;

namespace CoApp.Updater.ViewModel
{
    public abstract class ScreenViewModel : ViewModelBase
    {
        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }

        private string _subTitle;

        public string SubTitle
        {
            get { return _subTitle; }
            set
            {
                _subTitle = value;
                RaisePropertyChanged("SubTitle");
            }
        }

        public virtual void FireLoad()
        {
        }


        public virtual void FireUnload()
        {
        }


        private bool? _canInstall;

        public bool? CanInstall
        {
            get
            {
                if (_canInstall == null)
                {
                    ViewModelLocator.PolicyServiceStatic.InstallPolicy.ContinueWith(t => CanInstall = t.Result != PolicyResult.Other);
                }
                return _canInstall;
            }
            private set
            {
                _canInstall = value;
                RaisePropertyChanged("CanInstall");
            }
        }

        

    }
}
