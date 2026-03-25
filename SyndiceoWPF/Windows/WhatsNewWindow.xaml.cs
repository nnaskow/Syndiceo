using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for WhatsNewWindow.xaml
    /// </summary>
    public partial class WhatsNewWindow : Window
    {
        public WhatsNewWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           version.Text= $"версия {Properties.Settings.Default.appVersion}";
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WhatsNewNeeded = false;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
