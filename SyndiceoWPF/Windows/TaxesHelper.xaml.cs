using Syndiceo.Data.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Syndiceo.Windows
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
                // 1️⃣ Общо за входа
                decimal total = context.EntranceTransactions
                    .Where(t => t.EntranceId == entranceId && t.CategoryId == cat.Id)
                    .Select(t => t.Amount)
                    .FirstOrDefault();

                OriginalAmounts[cat.Id] = total;

                // 2️⃣ Разпределено досега
                decimal assigned = context.ApartmentTransactions
                    .Where(t => apartments.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                    .Sum(t => t.Amount);

                AssignedAmounts[cat.Id] = assigned;

                // 3️⃣ Остатък = Общо - Разпределено
                decimal remaining = total - assigned;
                RemainingAmounts[cat.Id] = remaining;

                // 4️⃣ Показваме в UI (например в Label или TextBlock)
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

            // delta = нова - стара
            // Ако delta е положително → апартаментът е увеличил → остатъкът намалява
            // Ако delta е отрицателно → апартаментът е намалил → остатъкът се увеличава
            RemainingAmounts[categoryId] -= delta;

            // Не допускаме отрицателни стойности
            if (RemainingAmounts[categoryId] < 0)
                RemainingAmounts[categoryId] = 0;

            // Визуално обновяване
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

