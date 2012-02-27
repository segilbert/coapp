using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CoApp.Updater.ViewModel;

namespace CoApp.Updater.Support
{
    public class PrimaryViewTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {

            var element = container as FrameworkElement;
            if (element != null && item != null && item is PrimaryViewModel)
            {
                var vm = (PrimaryViewModel) item;

                if (vm.Errors != null && vm.Errors.Any())
                {
                    return element.FindResource("ErrorTemplate") as DataTemplate;
                }

                if (vm.NumberOfProducts == 0)
                {
                    return element.FindResource("NoUpdatesTemplate") as DataTemplate;
                }

                else if (vm.NumberOfProducts > 0)
                {
                    return element.FindResource("UpdatesAvailableTemplate") as DataTemplate;
                }

            }
            return null;
            
        }
    }
}
