using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoApp.Toolkit.Win32;

namespace CoApp.Updater.Controls
{
    /// <summary>
    /// Interaction logic for ElevateShield.xaml
    /// </summar>y
    public partial class ElevateShield : UserControl
    {
       public BitmapSource ShieldIconSource { get; set; }

       public ElevateShield()
       { 
            Icon shield = null;
            if (WindowsVersionInfo.IsVistaOrPrior)
            {
                shield = SystemIcons.Shield;
                ShieldIconSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shield.Handle, Int32Rect.Empty,
                                                                                   BitmapSizeOptions.FromEmptyOptions());
            }
            else
            {
                //we have to get the right one for Win7
                ShieldIconSource = new BitmapImage(new Uri("pack://application:,,,/CoApp.Updater;component/Resources/UAC-Win7.png"));
            }
            
                
            this.InitializeComponent();
        }
    }
}
