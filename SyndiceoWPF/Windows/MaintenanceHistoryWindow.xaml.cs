using Syndiceo.Data.Models;
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
                !decimal.TryParse(AmountTextBox.Text, out decimal price))
            {
                MessageBox.Show("Моля, въведете валидни данни.");
                return;
            }

            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    if (_editingMaintenanceId == 0)
                    {
                        var newMaintenance = new Maintenance
                        {
                            Description = DescriptionTextBox.Text,
                            Price = (int)price,
                            DateOfMaintenance = DatePicker.SelectedDate ?? DateTime.Now,
                            EntranceId = _selectedEntrance.EntranceId
                        };
                        context.Maintenances.Add(newMaintenance);
                    }
                    else
                    {
                        var existing = context.Maintenances.Find(_editingMaintenanceId);
                        if (existing != null)
                        {
                            existing.Description = DescriptionTextBox.Text;
                            existing.Price = (int)price;
                            existing.DateOfMaintenance = DatePicker.SelectedDate ?? DateTime.Now;
                        }
                    }

                    context.SaveChanges();
                }

                _editingMaintenanceId = 0;
                DescriptionTextBox.Clear();
                AmountTextBox.Clear();
                DatePicker.SelectedDate = DateTime.Today;

                LoadMaintenanceData();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка: " + ex.Message);
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

        private int _editingMaintenanceId = 0;

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Maintenance m)
            {
                DescriptionTextBox.Text = m.Description;
                AmountTextBox.Text = m.Price.ToString();
                DatePicker.SelectedDate = m.DateOfMaintenance;

                _editingMaintenanceId = m.MaintenanceId;

            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Maintenance m)
            {
                if (MessageBox.Show("Изтриване на записа?", "Потвърждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var context = new SyndiceoDBContext())
                    {
                        var dbItem = context.Maintenances.Find(m.MaintenanceId);
                        if (dbItem != null)
                        {
                            context.Maintenances.Remove(dbItem);
                            context.SaveChanges();
                        }
                    }
                    LoadMaintenanceData();
                }
            }
        }
        private void MaintenanceDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var maintenance = e.Row.Item as Maintenance;
                if (maintenance != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            using (var context = new SyndiceoDBContext())
                            {
                                context.Maintenances.Update(maintenance);
                                context.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Грешка при автоматичен запис: " + ex.Message);
                            LoadMaintenanceData();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }
    }
}
