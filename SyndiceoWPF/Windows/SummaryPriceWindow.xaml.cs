using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            var apartment = context.Apartments.Include(a => a.Entrance).FirstOrDefault(a => a.ApartmentId == apartmentId);
            if (apartment == null) return;

            decimal transExpenses = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind != "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal transIncomes = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind == "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == apartmentId);
            decimal currentMonthTotal = debt?.TotalSum ?? 0;
            decimal currentMonthPaid = debt?.PaidSum ?? 0;

            decimal finalTotal = (transExpenses > 0) ? transExpenses : currentMonthTotal;
            decimal finalPaid = transIncomes + currentMonthPaid;

            _apartmentVm = new ApartmentViewModel
            {
                ApartmentId = apartmentId,
                TotalSum = finalTotal,
                PaidAmount = finalPaid,
                Entrance = new EntranceViewModel { Id = apartment.EntranceId }
            };

            TotalAmountTextBlock.Text = _apartmentVm.TotalSum.ToString("F2");
            CurrentPaidTextBlock.Text = _apartmentVm.PaidAmount.ToString("N2");
            RemainingSum.Text = _apartmentVm.RemainingSum.ToString("N2");
            UpdateProgressBar();
        }
        private void SavePayment_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(PaidAmountTextBox.Text, out decimal amount)) return;

            using var context = new SyndiceoDBContext();
            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == _apartmentVm.ApartmentId);

            decimal leftToPay = amount;

            if (debt != null)
            {
                decimal currentMonthOwed = debt.TotalSum - debt.PaidSum;
                decimal coveringCurrent = Math.Min(leftToPay, currentMonthOwed);

                debt.PaidSum += coveringCurrent;
                leftToPay -= coveringCurrent;
            }

            if (leftToPay > 0)
            {
                var incomeCat = context.Categories.FirstOrDefault(c => c.Kind == "Приход");
                context.ApartmentTransactions.Add(new ApartmentTransaction
                {
                    ApartmentId = _apartmentVm.ApartmentId,
                    Amount = leftToPay,
                    CategoryId = incomeCat?.Id ?? 0,
                    Description = "Покриване на несъбрани такси",
                    TransDate = DateOnly.FromDateTime(DateTime.Now)
                });
            }

            context.SaveChanges();
            AddToTotalCollected(amount);

            this.DialogResult = true;
            this.Close();
        }
        private void AddToTotalCollected(decimal amount)
        {
            if (_apartmentVm == null || _apartmentVm.Entrance == null || amount <= 0) return;

            using var context = new SyndiceoDBContext();
            var entranceId = _apartmentVm.Entrance.Id;

            var collectedCategory = context.Categories
                .FirstOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход" && c.Appliance == "entrances");

            if (collectedCategory == null)
            {
                collectedCategory = new Category { Name = "Събрани такси", Kind = "Приход", Appliance = "entrances" };
                context.Categories.Add(collectedCategory);
                context.SaveChanges();
            }

            var entranceTransaction = context.EntranceTransactions
                .FirstOrDefault(et => et.EntranceId == entranceId && et.CategoryId == collectedCategory.Id);

            if (entranceTransaction == null)
            {
                context.EntranceTransactions.Add(new EntranceTransaction
                {
                    EntranceId = entranceId,
                    CategoryId = collectedCategory.Id,
                    Amount = amount,
                    Description = "Събрани такси за текущия период"
                });
            }
            else
            {
                entranceTransaction.Amount += amount;
            }

            context.SaveChanges();
        }
        private void PaidAmountTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (SessionData.LastPayments.IsNullOrEmpty())
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void UpdateProgressBar()
        {
            if (_apartmentVm == null || _apartmentVm.TotalSum <= 0) return;

            double targetValue = (double)(_apartmentVm.PaidAmount / _apartmentVm.TotalSum) * 100;
            if (targetValue > 100) targetValue = 100;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            PaymentProgressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }
        private void PayAllButton_Click(object sender, RoutedEventArgs e)
        {
            string rawText = RemainingSum.Text.Replace(" ", "");

            if (decimal.TryParse(rawText.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal remainingSum))
            {
                if (remainingSum <= 0)
                {
                    MessageBox.Show("Няма сума за плащане.", "Информация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                PaidAmountTextBox.Text = remainingSum.ToString("F2");

                SavePayment_Click(sender, e);
            }
            else
            {
                MessageBox.Show("Невалидна сума в полето за остатък.");
            }
        }
    }
}
