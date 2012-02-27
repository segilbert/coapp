using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoApp.Updater.Model;
using CoApp.Updater.Model.Interfaces;

namespace CoApp.Updater.ViewModel
{
    class UpdatingViewModel : ScreenViewModel
    {
        internal IUpdateService UpdateService;
        public UpdatingViewModel()
        {
            Title = "CoApp Update";

            UpdateService = new ViewModelLocator().UpdateService;
            var updates = UpdateService.CheckForUpdates();
            updates.ContinueWith((t) => new ViewModelLocator().NavigationService.GoTo(new PrimaryViewModel()));
            updates.Start();
        }


        
    }
}
