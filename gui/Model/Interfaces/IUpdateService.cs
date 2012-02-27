using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoApp.Updater.Model.Interfaces
{
    public interface IUpdateService
    {
        Task CheckForUpdates();

        //IEnumerable<string> Warnings { get; }
        int NumberOfProducts { get; set; }
        void SetSelectedProducts(IEnumerable<string> products); 
        
        Task PerformInstallation();


    }
}
