using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CoApp.Updater.Messages;
using CoApp.Updater.Model.Interfaces;
using CoApp.Updater.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace CoApp.Updater.Model
{
    [XmlRoot(ElementName = "Navigation", Namespace = "http://coapp.org/smartrestart-1.0")]
    public class NavigationService : INavigationService
    {
        private Stack<ScreenViewModel> _innerStack = new Stack<ScreenViewModel>();
       
        public void GoTo(ScreenViewModel viewModel)
        {
            _innerStack.Push(viewModel);
            Messenger.Default.Send(new GoToMessage {Destination = viewModel});
        }

        public void Back()
        {
            _innerStack.Pop();
            Messenger.Default.Send(new GoToMessage {Destination = _innerStack.Peek()});
        }
        
        [XmlArray]
        public ReadOnlyCollection<ScreenViewModel> Stack
        {
            get { return new ReadOnlyCollection<ScreenViewModel>(_innerStack.ToArray()); }
        }

        [XmlIgnore]
        public bool StackEmpty
        {
            get { return _innerStack.Count <= 1; }
        }
    }
}
