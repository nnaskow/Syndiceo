using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static Syndiceo.Windows.ManagementWindow;

namespace Syndiceo.Windows
{
    public partial class SummaryPriceWindow : Window
    {
        private ApartmentViewModel _apartmentVm;

        public SummaryPriceWindow(int apartmentId)
        {
            InitializeComponent();
            LoadApartmentData(apartmentId);
        }

        private void LoadApartmentData(int apartmentId)
        {
            using var context = new SyndiceoDBContext();

            var apartment = context.Apartments
                .Include(a => a.Owner)
                .Include(a => a.Entrance)
                .FirstOrDefault(a => a.ApartmentId == apartmentId);

            if (apartment == null)
            {
                MessageBox.Show("Апартаментът не е намерен.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var totalExpenses = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind != "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            var totalIncomes = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind == "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal remaining = totalExpenses - totalIncomes;

            var paid = context.Debts
                .Where(d => d.ApartmentId == apartmentId)
                .Select(d => (decimal?)d.PaidSum)
                .FirstOrDefault() ?? 0;

            _apartmentVm = new ApartmentViewModel
            {
                ApartmentId = apartment.ApartmentId,
                ApartmentNumber = apartment.ApartmentNumber,
                OwnerName = apartment.Owner?.OwnerName,
                Entrance = new EntranceViewModel
                {
                    Id = apartment.Entrance.EntranceId,
                    Name = apartment.Entrance.EntranceName
                },
                TotalSum = remaining,
                PaidAmount = paid
            };            
            TotalAmountTextBlock.Text = $"{_apartmentVm.TotalSum:F2}";
            CurrentPaidTextBlock.Text = $"{_apartmentVm.PaidAmount:F2}";
            RemainingSum.Text = $"{(_apartmentVm.TotalSum - _apartmentVm.PaidAmount)}";
        }
        private void UpdateDebtForApartment(int apartmentId, decimal newlyPaid)
        {
            using var context = new SyndiceoDBContext();
            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == apartmentId);

            if (debt == null)
            {
                debt = new Debt
                {
                    ApartmentId = apartmentId,
                    TotalSum = context.ApartmentTransactions
                                      .Where(t => t.ApartmentId == apartmentId && t.Category.Kind != "Приход")
                                      .Sum(t => (decimal?)t.Amount) ?? 0,
                    PaidSum = newlyPaid
                };
                context.Debts.Add(debt);
            }
            else
            {
                debt.PaidSum += newlyPaid; 
                if (debt.PaidSum > debt.TotalSum)
                    debt.PaidSum = debt.TotalSum;
            }

            context.SaveChanges();
        }

        private void SavePayment_Click(object sender, RoutedEventArgs e)
        {
            if (_apartmentVm == null) return;

            if (!decimal.TryParse(PaidAmountTextBox.Text, out decimal paid) || paid <= 0)
            {
                MessageBox.Show("Моля, въведете валидна сума за плащане.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var context = new SyndiceoDBContext();

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == _apartmentVm.ApartmentId);
            if (debt == null)
            {
                debt = new Debt
                {
                    ApartmentId = _apartmentVm.ApartmentId,
                    TotalSum = context.ApartmentTransactions
                                       .Where(t => t.ApartmentId == _apartmentVm.ApartmentId && t.Category.Kind != "Приход")
                                       .Sum(t => (decimal?)t.Amount) ?? 0,
                    PaidSum = paid
                };
                context.Debts.Add(debt);
            }
            else
            {
                debt.PaidSum += paid;
                if (debt.PaidSum > debt.TotalSum)
                    debt.PaidSum = debt.TotalSum;
            }

            context.SaveChanges();

            AddToTotalCollected(paid);

            _apartmentVm.UpdatePayment(paid);
            CurrentPaidTextBlock.Text = $"{_apartmentVm.PaidAmount:F2}";
            RemainingSum.Text = $"{_apartmentVm.RemainingSum:F2}";

            SessionData.LastPayments.Add(new PaymentRecord
            {
                ApartmentId = _apartmentVm.ApartmentId,
                Amount = paid,
                DebtId = debt.Id
            });

            Properties.Settings.Default.areThereAnyLastPayments = true;
            Properties.Settings.Default.Save();

            MessageBox.Show("Плащането е записано успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        private void AddToTotalCollected(decimal amount)
        {
            if (_apartmentVm == null || amount <= 0) return;

            using var context = new SyndiceoDBContext();

            var entranceId = _apartmentVm.Entrance.Id;

            var collectedCategory = context.Categories
                .FirstOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход" && c.Appliance == "entrances");

            if (collectedCategory == null)
            {
                collectedCategory = new Category
                {
                    Name = "Събрани такси",
                    Kind = "Приход",
                    Appliance = "entrances"
                };
                context.Categories.Add(collectedCategory);
                context.SaveChanges(); 
            }

            var entranceTransaction = context.EntranceTransactions
                .FirstOrDefault(et => et.EntranceId == entranceId && et.CategoryId == collectedCategory.Id);

            if (entranceTransaction == null)
            {
                entranceTransaction = new EntranceTransaction
                {
                    EntranceId = entranceId,
                    CategoryId = collectedCategory.Id,
                    Amount = amount,
                    Description = "Събрани такси"
                };
                context.EntranceTransactions.Add(entranceTransaction);
            }
            else
            {
                entranceTransaction.Amount += amount;
            }

            context.SaveChanges();
        }

        private void PaidAmountTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key== System.Windows.Input.Key.Enter)
            {
                SavePayment_Click(sender, e);
            }    
        }

        LastTransactionsWindow lst = new LastTransactionsWindow(SessionData.LastPayments);
        private void LastTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            lst.ShowDialog();
            LoadApartmentData(_apartmentVm.ApartmentId);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PaidAmountTextBox.Text))
                return;

            if (!decimal.TryParse(PaidAmountTextBox.Text, out decimal enteredAmount))
                return;

            if (enteredAmount <= _apartmentVm.PaidAmount)
                return;

            decimal newlyPaid = enteredAmount - _apartmentVm.PaidAmount;

            var result = MessageBox.Show(
                $"Имате незаписано плащане: {newlyPaid:F2}. Искате ли да го запишете преди затваряне?",
                "Незавършено плащане",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                SavePaymentWithoutClosing(newlyPaid);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true; 
            }
        }

        private void SavePaymentWithoutClosing(decimal newlyPaid)
        {
            if (newlyPaid <= 0) return;

            using var context = new SyndiceoDBContext();

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == _apartmentVm.ApartmentId);
            if (debt == null)
            {
                debt = new Debt
                {
                    ApartmentId = _apartmentVm.ApartmentId,
                    TotalSum = context.ApartmentTransactions
                                       .Where(t => t.ApartmentId == _apartmentVm.ApartmentId && t.Category.Kind != "Приход")
                                       .Sum(t => (decimal?)t.Amount) ?? 0,
                    PaidSum = newlyPaid
                };
                context.Debts.Add(debt);
            }
            else
            {
                debt.PaidSum += newlyPaid;
                if (debt.PaidSum > debt.TotalSum)
                    debt.PaidSum = debt.TotalSum;
            }

            context.SaveChanges();

            var entranceId = _apartmentVm.Entrance.Id;

            var collectedCategory = context.Categories
                .FirstOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход" && c.Appliance == "entrances");

            if (collectedCategory == null)
            {
                collectedCategory = new Category
                {
                    Name = "Събрани такси",
                    Kind = "Приход",
                    Appliance = "entrances"
                };
                context.Categories.Add(collectedCategory);
                context.SaveChanges();
            }

            var entranceTransaction = context.EntranceTransactions
                .FirstOrDefault(et => et.EntranceId == entranceId && et.CategoryId == collectedCategory.Id);

            if (entranceTransaction == null)
            {
                entranceTransaction = new EntranceTransaction
                {
                    EntranceId = entranceId,
                    CategoryId = collectedCategory.Id,
                    Amount = newlyPaid,
                    Description = "Събрани такси"
                };
                context.EntranceTransactions.Add(entranceTransaction);
            }
            else
            {
                entranceTransaction.Amount += newlyPaid;
            }

            context.SaveChanges();

            _apartmentVm.UpdatePayment(newlyPaid);
            CurrentPaidTextBlock.Text = $"{_apartmentVm.PaidAmount:F2}";
            RemainingSum.Text = $"{_apartmentVm.RemainingSum:F2}";

            SessionData.LastPayments.Add(new PaymentRecord
            {
                ApartmentId = _apartmentVm.ApartmentId,
                Amount = newlyPaid,
                DebtId = debt.Id
            });

            Properties.Settings.Default.areThereAnyLastPayments = true;
            Properties.Settings.Default.Save();

            MessageBox.Show("Плащането е записано успешно и сумата е добавена към събраните такси за входа!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(SessionData.LastPayments.IsNullOrEmpty())
            {
                LastTransactionsButton.IsEnabled = false;
                LastTransactionsButton.Opacity = 0.375;
            }
            else
            {
                LastTransactionsButton.IsEnabled = true;
                LastTransactionsButton.Opacity = 1;

            }
        }
    }
}
