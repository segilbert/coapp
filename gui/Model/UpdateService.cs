using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoApp.Toolkit.Engine.Client;
using CoApp.Updater.Model.Interfaces;

namespace CoApp.Updater.Model
{
    public class UpdateService : IUpdateService
    {
        internal PackageManager Pm;
        
        public UpdateService()
        {
            Pm = PackageManager.Instance;
        }


        public Task CheckForUpdates()
        {
            return new Task(PerformUpdates);
        }

        public int NumberOfProducts
        {
            get { return 5; }
            set { throw new NotImplementedException(); }
        }

        public IEnumerable<string> Warnings
        {
            get { throw new NotImplementedException(); }
        }

        public void SetSelectedProducts(IEnumerable<string> products)
        {
            throw new NotImplementedException();
        }

        public Task BlockProduct(IEnumerable<string> products)
        {
            return null;
        }

        public Task PerformInstallation()
        {
            throw new NotImplementedException();
        }


        private void PerformUpdates()
        {
            Thread.Sleep(2000);
        }
    }
}
