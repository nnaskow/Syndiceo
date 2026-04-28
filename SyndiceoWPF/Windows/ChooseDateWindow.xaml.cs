using DocumentFormat.OpenXml.Spreadsheet;
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
    /// Interaction logic for ChooseDateWindow.xaml
    /// </summary>
    public partial class ChooseDateWindow : Window
    {
        public ChooseDateWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;

            for (int i = currentYear - 5; i <= currentYear + 1; i++)
            {
                YearComboBox.Items.Add(i);
            }

            var allMonths = new List<KeyValuePair<int, string>>()
    {
        new KeyValuePair<int, string>(1, "Януари"),
        new KeyValuePair<int, string>(2, "Февруари"),
        new KeyValuePair<int, string>(3, "Март"),
        new KeyValuePair<int, string>(4, "Април"),
        new KeyValuePair<int, string>(5, "Май"),
        new KeyValuePair<int, string>(6, "Юни"),
        new KeyValuePair<int, string>(7, "Юли"),
        new KeyValuePair<int, string>(8, "Август"),
        new KeyValuePair<int, string>(9, "Септември"),
        new KeyValuePair<int, string>(10, "Октомври"),
        new KeyValuePair<int, string>(11, "Ноември"),
        new KeyValuePair<int, string>(12, "Декември")
    };
            var filteredMonths = new List<KeyValuePair<int, string>>();
            for (int i = -3; i <= 3; i++)
            {
                int targetMonth = currentMonth + i;

                if (targetMonth < 1) targetMonth += 12;
                if (targetMonth > 12) targetMonth -= 12;

                var m = allMonths.First(x => x.Key == targetMonth);
                if (!filteredMonths.Any(x => x.Key == m.Key))
                {
                    filteredMonths.Add(m);
                }
            }

            MonthComboBox.ItemsSource = filteredMonths;
            MonthComboBox.DisplayMemberPath = "Value";
            MonthComboBox.SelectedValuePath = "Key";

            MonthComboBox.SelectedValue = currentMonth;

            YearComboBox.SelectedItem = currentYear;
        }

        private void ChooseDateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonthComboBox.SelectedValue != null)
            {
                Properties.Settings.Default.monthForPrinting = MonthComboBox.SelectedValue.ToString();
            }

            if (YearComboBox.SelectedItem != null)
            {
                Properties.Settings.Default.yearForPrinting = YearComboBox.SelectedItem.ToString();
            }

            Properties.Settings.Default.Save();
            this.DialogResult = true;
            this.Close();
        }

    }
}
