using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using DocumentFormat.OpenXml.Office2010.PowerPoint;

namespace Syndiceo.Windows
{
    public partial class LastTransactionsWindow : Window
    {
        private readonly List<PaymentRecord> _payments;

        public LastTransactionsWindow(List<PaymentRecord> payments)
        {
            InitializeComponent();
            _payments = payments;
            TransactionsList.ItemsSource = _payments;
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PaymentRecord record)
            {
                var confirm = MessageBox.Show(
                    $"Сигурни ли сте, че искате да премахнете плащането ({record.Amount:F2} лв.)?",
                    "Потвърждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                try
                {
                    using var context = new SyndiceoDBContext();
                    var debt = context.Debts.FirstOrDefault(d => d.Id == record.DebtId);
                    if (debt != null)
                    {
                        // ⏪ Връщаме сумата
                        debt.PaidSum -= record.Amount;
                        if (debt.PaidSum < 0)
                            debt.PaidSum = 0;

                        context.SaveChanges();
                    }

                    _payments.Remove(record);
                    SessionData.LastPayments.Remove(record);
                    TransactionsList.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Грешка при изтриване: " + ex.Message, "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(TransactionsList.Items.Count==0)
            {
                Properties.Settings.Default.areThereAnyLastPayments = false;

            }
            else
            {
                Properties.Settings.Default.areThereAnyLastPayments = true;
            }
            Properties.Settings.Default.Save();
        }
    }
}
