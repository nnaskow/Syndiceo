using Syndiceo.Models;
using Syndiceo.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Syndiceo.Data.Models;
using Syndiceo.Data;
namespace Archon
{
    public partial class TaxesHelper : Window
    {
        public ObservableCollection<CategoryViewModel> IncomeCategories { get; }
        public ObservableCollection<CategoryViewModel> ExpenseCategories { get; }

        private Dictionary<int, decimal> RemainingAmounts = new();
        private Dictionary<int, decimal> OriginalAmounts = new();
        private Dictionary<int, decimal> AssignedAmounts = new();
        public bool IsReadOnlyMode { get; set; } = true;

        public TaxesHelper(ObservableCollection<CategoryViewModel> income,
                           ObservableCollection<CategoryViewModel> expense,
                           int entranceId,
                           Window owner)
        {
            IncomeCategories = income;
            ExpenseCategories = expense;
            Owner = owner;

            InitializeComponent();

            IncomeCategoriesPanel.ItemsSource = IncomeCategories;
            ExpenseCategoriesPanel.ItemsSource = ExpenseCategories;

            LoadCategoryData(entranceId);

        }

        private void LoadCategoryData(int entranceId)
        {
            using var context = new SyndiceoDBContext();

            var apartments = context.Apartments
                .Where(a => a.EntranceId == entranceId)
                .Select(a => a.ApartmentId)
                .ToList();

            foreach (var cat in IncomeCategories.Concat(ExpenseCategories))
            {
                decimal total = context.EntranceTransactions
                    .Where(t => t.EntranceId == entranceId && t.CategoryId == cat.Id)
                    .Select(t => t.Amount)
                    .FirstOrDefault();

                OriginalAmounts[cat.Id] = total;

                decimal assigned = context.ApartmentTransactions
                    .Where(t => apartments.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                    .Sum(t => t.Amount);

                AssignedAmounts[cat.Id] = assigned;

                decimal remaining = total - assigned;
                RemainingAmounts[cat.Id] = remaining;

                cat.Amount = remaining.ToString("N2");
            }
        }

        /// <summary>
        /// Актуализира оставащата сума при промяна на апартамент.
        /// delta = (нова - стара)
        /// </summary>
        public void AdjustRemainingAmount(int categoryId, decimal delta)
        {
            if (!RemainingAmounts.ContainsKey(categoryId))
                return;
            RemainingAmounts[categoryId] -= delta;
            if (RemainingAmounts[categoryId] < 0)
                RemainingAmounts[categoryId] = 0;
            var cat = IncomeCategories.Concat(ExpenseCategories)
                .FirstOrDefault(c => c.Id == categoryId);

            if (cat != null)
                cat.Amount = RemainingAmounts[categoryId].ToString("N2");
        }


        /// <summary>
        /// Връща текущите остатъци (за запис при нужда)
        /// </summary>
        public Dictionary<int, decimal> GetRemainingAmounts() => new(RemainingAmounts);
    }
}

