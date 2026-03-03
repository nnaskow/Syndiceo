using Syndiceo.Models;
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
using Syndiceo.Data.Models;
namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for MaintenanceHistoryWindow.xaml
    /// </summary>
    public partial class MaintenanceHistoryWindow : Window
    {
        private readonly Entrance _selectedEntrance;
        public MaintenanceHistoryWindow(Entrance entrance)
        {
            InitializeComponent();
            _selectedEntrance = entrance;
            DatePicker.SelectedDate = DateTime.Today;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                    string.IsNullOrWhiteSpace(AmountTextBox.Text) ||
                    !DatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Моля попълнете всички полета.", "Грешка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(AmountTextBox.Text, out int amount))
            {
                MessageBox.Show("Сумата трябва да е число.", "Грешка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var maintenance = new Maintenance
            {
                Description = DescriptionTextBox.Text,
                Price = amount,
                DateOfMaintenance = DatePicker.SelectedDate.Value,
                EntranceId = _selectedEntrance.EntranceId
            };

            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    context.Maintenances.Add(maintenance);
                    context.SaveChanges();

                    MaintenanceDataGrid.ItemsSource = context.Maintenances
                                                             .OrderByDescending(m => m.DateOfMaintenance)
                                                             .ToList();
                }
                LoadMaintenanceData();

                MessageBox.Show("Ремонтната дейност е добавена успешно!",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DescriptionTextBox.Clear();
                AmountTextBox.Clear();
                DatePicker.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Възникна грешка: " + ex.Message,
                                "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMaintenanceData();
        }
        private void LoadMaintenanceData()
        {
            using (var context = new SyndiceoDBContext())
            {
                var maintenances = context.Maintenances
                    .Where(m => m.EntranceId == _selectedEntrance.EntranceId)
                    .OrderByDescending(m => m.DateOfMaintenance)
                    .ToList();

                MaintenanceDataGrid.ItemsSource = maintenances;
            }
        }

    }
}
