using Syndiceo.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Syndiceo.Data.Models;

namespace Syndiceo.Windows
{
    public partial class TaxesWindow : Window
    {
        private int? _apartmentId;
        private int? _entranceId;

        private ObservableCollection<CategoryViewModel> _apartmentIncomeCategories = new();
        private ObservableCollection<CategoryViewModel> _apartmentExpenseCategories = new();
        private ObservableCollection<CategoryViewModel> _entranceIncomeCategories = new();
        private ObservableCollection<CategoryViewModel> _entranceExpenseCategories = new();

        private ObservableCollection<CategoryViewModel> _selectedIncomeCategories = new ObservableCollection<CategoryViewModel>();
        private ObservableCollection<CategoryViewModel> _selectedExpenseCategories = new ObservableCollection<CategoryViewModel>();

        private TaxesHelper _helper;

        public TaxesWindow(int? apartmentId = null, int? entranceId = null)
        {
            InitializeComponent();
            _apartmentId = apartmentId;
            _entranceId = entranceId;

            if (_apartmentId.HasValue)
            {
                this.Title = "Управление на такси за апартамент";
                _selectedIncomeCategories = _apartmentIncomeCategories;
                _selectedExpenseCategories = _apartmentExpenseCategories;

                cashboxLabel.Opacity = 0.375;
                Cashbox.IsReadOnly = true;
                Cashbox.Opacity = 0.375;
            }
            else if (_entranceId.HasValue)
            {
                this.Title = "Управление на такси за вход";
                _selectedIncomeCategories = _entranceIncomeCategories;
                _selectedExpenseCategories = _entranceExpenseCategories;
            }

            IncomeCategoriesPanel.ItemsSource = _selectedIncomeCategories;
            ExpenseCategoriesPanel.ItemsSource = _selectedExpenseCategories;

            LoadCategories();
            LoadExistingTransactions();

            if (_entranceId.HasValue)
            {
                using var context = new SyndiceoDBContext();
                var cashbox = context.Cashboxes.FirstOrDefault(c => c.EntranceId == _entranceId.Value);
                if (cashbox != null)
                    Cashbox.Text = cashbox.CurrentBalance.ToString("N2");
            }
        }

        private void LoadExistingTransactions()
        {
            using var context = new SyndiceoDBContext();

            if (_apartmentId.HasValue)
            {
                var transactions = context.ApartmentTransactions
                    .Where(t => t.ApartmentId == _apartmentId.Value)
                    .Select(t => new { t.CategoryId, t.Amount, t.Category.Name, t.Category.Kind })
                    .ToList();

                foreach (var t in transactions)
                {
                    var vm = new CategoryViewModel
                    {
                        Id = t.CategoryId,
                        Name = t.Name,
                        Amount = t.Amount.ToString("N2")
                    };

                    if (!Properties.Settings.Default.taxesWdHelpNeeded)
                        SubscribeCategory(vm, _apartmentId.Value);

                    if (t.Kind == "Приход") _selectedIncomeCategories.Add(vm);
                    else _selectedExpenseCategories.Add(vm);
                }
            }

            else if (_entranceId.HasValue)
            {
                var transactions = context.EntranceTransactions
                    .Where(t => t.EntranceId == _entranceId.Value)
                    .Select(t => new { t.CategoryId, t.Amount, t.Category.Name, t.Category.Kind })
                    .ToList();

                foreach (var t in transactions)
                {
                    var vm = new CategoryViewModel
                    {
                        Id = t.CategoryId,
                        Name = t.Name,
                        Amount = t.Amount.ToString("N2")
                    };

                    if (t.Kind == "Приход") _selectedIncomeCategories.Add(vm);
                    else _selectedExpenseCategories.Add(vm);
                }
            }


            RefreshCategoryList();
        }

        private void LoadCategories()
        {
            RefreshCategoryList();
        }

        private void RefreshCategoryList()
        {
            using var context = new SyndiceoDBContext();
            var allCategories = context.Categories.OrderBy(c => c.Name).ToList();

            string categoryType = null;
            if (_apartmentId.HasValue)
                categoryType = "apartments";
            else if (_entranceId.HasValue)
                categoryType = "entrances";

            var filtered = allCategories
                .Where(c =>
                    (categoryType == null || c.Appliance == categoryType) &&
                    !_selectedIncomeCategories.Any(ic => ic.Id == c.Id) &&
                    !_selectedExpenseCategories.Any(ec => ec.Id == c.Id))
                .ToList();

            CategoryListBox.ItemsSource = filtered;
        }


        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            string name = CategoryTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
                return;

            string kind = (CategoryKindComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Разход";

            using var context = new SyndiceoDBContext();

            // Определяме типа според наличието на ids, не по Title
            string categoryType = null;
            if (_apartmentId.HasValue)
                categoryType = "apartments";
            else if (_entranceId.HasValue)
                categoryType = "entrances";

            var newCategory = new Syndiceo.Data.Models.Category
            {
                Name = name,
                Kind = kind,
                Appliance = categoryType
            };

            context.Categories.Add(newCategory);
            context.SaveChanges();

            CategoryTextBox.Clear();
            CategoryKindComboBox.SelectedIndex = 0;

            LoadCategories();
        }

        private void addCatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is Syndiceo.Data.Models.Category selectedCategory)
                AddCategoryToPanel(selectedCategory);
        }

        private void CategoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CategoryListBox.SelectedItem is Syndiceo.Data.Models.Category selectedCategory)
                AddCategoryToPanel(selectedCategory);
        }

        private void AddCategoryToPanel(Syndiceo.Data.Models.Category selectedCategory)
        {
            var vm = new CategoryViewModel { Id = selectedCategory.Id, Name = selectedCategory.Name, Amount = "0" };

            if (_apartmentId.HasValue && !Properties.Settings.Default.taxesWdHelpNeeded)
                SubscribeCategory(vm, _apartmentId.Value);

            if (selectedCategory.Kind == "Приход")
                _selectedIncomeCategories.Add(vm);
            else
                _selectedExpenseCategories.Add(vm);

            RefreshCategoryList();
        }


        private void RemoveIncomeCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CategoryViewModel cat)
            {
                _selectedIncomeCategories.Remove(cat);
                RemoveCategoryFromDatabase(cat);
                RefreshCategoryList();
            }
        }

        private void RemoveExpenseCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CategoryViewModel cat)
            {
                _selectedExpenseCategories.Remove(cat);
                RemoveCategoryFromDatabase(cat);
                RefreshCategoryList();
            }
        }

        private void RemoveCategoryFromDatabase(CategoryViewModel cat)
        {
            using var context = new SyndiceoDBContext();

            if (_apartmentId.HasValue)
            {
                var existing = context.ApartmentTransactions
                    .FirstOrDefault(t => t.ApartmentId == _apartmentId.Value && t.CategoryId == cat.Id);
                if (existing != null)
                {
                    context.ApartmentTransactions.Remove(existing);
                    context.SaveChanges();
                }
            }
            else if (_entranceId.HasValue)
            {
                var apartments = context.Apartments
                    .Where(a => a.EntranceId == _entranceId.Value)
                    .Select(a => a.ApartmentId)
                    .ToList();

                var transactionsToRemove = context.ApartmentTransactions
                    .Where(t => apartments.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                    .ToList();
                context.ApartmentTransactions.RemoveRange(transactionsToRemove);

                var entranceTransaction = context.EntranceTransactions
                    .FirstOrDefault(t => t.EntranceId == _entranceId.Value && t.CategoryId == cat.Id);
                if (entranceTransaction != null)
                    context.EntranceTransactions.Remove(entranceTransaction);

                context.SaveChanges();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка за избрани категории
            if ((_selectedIncomeCategories.Count == 0 && _selectedExpenseCategories.Count == 0) &&
                (_helper == null || (_helper.IncomeCategories.Count == 0 && _helper.ExpenseCategories.Count == 0)))
            {
                MessageBox.Show("Няма избрани категории!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var context = new SyndiceoDBContext();

            var categoriesToSave =
                (_helper != null && !_helper.IsReadOnlyMode && (_helper.IncomeCategories.Any() || _helper.ExpenseCategories.Any()))
                    ? _helper.IncomeCategories.Concat(_helper.ExpenseCategories)
                    : _selectedIncomeCategories.Concat(_selectedExpenseCategories);

            if (_apartmentId.HasValue)
            {
                foreach (var cat in categoriesToSave)
                {
                    var existing = context.ApartmentTransactions
                        .FirstOrDefault(t => t.ApartmentId == _apartmentId.Value && t.CategoryId == cat.Id);

                    if (existing != null)
                        existing.Amount = cat.GetDecimalAmount();
                    else
                        context.ApartmentTransactions.Add(new ApartmentTransaction
                        {
                            ApartmentId = _apartmentId.Value,
                            CategoryId = cat.Id,
                            Amount = cat.GetDecimalAmount(),
                            TransDate = DateOnly.FromDateTime(DateTime.Now)
                        });

                    // Обновяваме сумата за входа
                    UpdateEntranceCategoryTotal(cat);
                }

                context.SaveChanges();
                UpdateEntranceTotalSum(_apartmentId.Value);
                UpdateDebtForApartment(_apartmentId.Value);
            }
            else if (_entranceId.HasValue)
            {
                foreach (var cat in categoriesToSave)
                {
                    var existing = context.EntranceTransactions
                        .FirstOrDefault(t => t.EntranceId == _entranceId.Value && t.CategoryId == cat.Id);

                    if (existing != null)
                        existing.Amount = cat.GetDecimalAmount();
                    else
                        context.EntranceTransactions.Add(new EntranceTransaction
                        {
                            EntranceId = _entranceId.Value,
                            CategoryId = cat.Id,
                            Amount = cat.GetDecimalAmount(),
                            TransDate = DateOnly.FromDateTime(DateTime.Now)
                        });
                }

                // Запазваме и касата
                if (decimal.TryParse(Cashbox.Text.Replace('.', ','), out decimal balance))
                {                                         
                    var existingCashbox = context.Cashboxes.FirstOrDefault(c => c.EntranceId == _entranceId.Value);
                    if (existingCashbox != null)
                        existingCashbox.CurrentBalance = balance;
                    else
                        context.Cashboxes.Add(new Cashbox
                        {
                            EntranceId = _entranceId.Value,
                            CurrentBalance = balance
                        });
                }
                context.SaveChanges();
            }

            this.Close();
        }
        private void SubscribeCategory(CategoryViewModel cat, int apartmentId)
        {
            cat.AmountChanged += (s, e) =>
            {
                decimal delta = e.NewValue - e.OldValue;

                // Без синхронизация към EntranceTransactions
                _helper?.AdjustRemainingAmount(cat.Id, delta);
            };
        }

        private void UnsubscribeCategory(CategoryViewModel cat)
        {
            cat.ClearAmountChangedSubscribers();
        }

        private void CategoryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddCategoryButton_Click(sender, e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void OpenTaxesHelperForEntrance(int entranceId)
        {
            using var context = new SyndiceoDBContext();

            var apartmentsInEntrance = context.Apartments
                .Where(a => a.EntranceId == entranceId)
                .Select(a => a.ApartmentId)
                .ToList();

            var allCategories = context.Categories.ToList();

            var incomeCats = new List<CategoryViewModel>();
            var expenseCats = new List<CategoryViewModel>();

            foreach (var cat in allCategories)
            {
                // Вземаме или създаваме EntranceTransaction
                var entranceTransaction = context.EntranceTransactions
                    .FirstOrDefault(t => t.EntranceId == entranceId && t.CategoryId == cat.Id);

                if (entranceTransaction == null)
                {
                    // Ако няма запис, създаваме с общо от апартаментите
                    decimal totalAssigned = context.ApartmentTransactions
                        .Where(t => apartmentsInEntrance.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                        .Sum(t => t.Amount);

                    entranceTransaction = new EntranceTransaction
                    {
                        EntranceId = entranceId,
                        CategoryId = cat.Id,
                        Amount = totalAssigned,
                        TransDate = DateOnly.FromDateTime(DateTime.Now)
                    };
                    context.EntranceTransactions.Add(entranceTransaction);
                }
            }

            context.SaveChanges(); // Важно: записваме преди helper-а!

            // Сега създаваме CategoryViewModel с остатъци
            foreach (var cat in allCategories)
            {
                decimal totalAssigned = context.ApartmentTransactions
                    .Where(t => apartmentsInEntrance.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                    .Sum(t => t.Amount);

                decimal totalAmount = context.EntranceTransactions
                    .Where(t => t.EntranceId == entranceId && t.CategoryId == cat.Id)
                    .Select(t => t.Amount)
                    .FirstOrDefault();

                decimal remaining = totalAmount - totalAssigned;

                var vm = new CategoryViewModel
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    Amount = remaining.ToString("N2")
                };

                if (cat.Kind == "Приход") incomeCats.Add(vm);
                else expenseCats.Add(vm);
            }

            _helper = new TaxesHelper(
                new ObservableCollection<CategoryViewModel>(incomeCats),
                new ObservableCollection<CategoryViewModel>(expenseCats),
                entranceId,
                this
            );
            _helper.IsReadOnlyMode = !_entranceId.HasValue;

            _helper.Owner = this;
            _helper.WindowStartupLocation = WindowStartupLocation.Manual;
            _helper.Left = this.Left + this.Width + 10;
            _helper.Top = this.Top;
            _helper.Show();
        }

        private void UpdateEntranceCategoryTotal(CategoryViewModel cat)
        {
            if (!_entranceId.HasValue)
                return;

            using var context = new SyndiceoDBContext();
            var apartments = context.Apartments
                .Where(a => a.EntranceId == _entranceId.Value)
                .Select(a => a.ApartmentId)
                .ToList();

            decimal total = context.ApartmentTransactions
                .Where(t => apartments.Contains(t.ApartmentId) && t.CategoryId == cat.Id)
                .Sum(t => t.Amount);

            var entranceTransaction = context.EntranceTransactions
                .FirstOrDefault(t => t.EntranceId == _entranceId.Value && t.CategoryId == cat.Id);

            if (entranceTransaction != null)
                entranceTransaction.Amount = total;
            else
                context.EntranceTransactions.Add(new EntranceTransaction
                {
                    EntranceId = _entranceId.Value,
                    CategoryId = cat.Id,
                    Amount = total,
                    TransDate = DateOnly.FromDateTime(DateTime.Now)
                });

            context.SaveChanges();
        }

        private void UpdateEntranceTotalSum(int apartmentId)
        {
            using var context = new SyndiceoDBContext();

            // Намери входа на апартамента
            var entranceId = context.Apartments
                .Where(a => a.ApartmentId == apartmentId)
                .Select(a => a.EntranceId)
                .FirstOrDefault();

            if (entranceId == 0)
                return;

            // Изчисли всички дължими суми по апартаменти във входа
            var apartments = context.Apartments
                .Where(a => a.EntranceId == entranceId)
                .Select(a => a.ApartmentId)
                .ToList();

            // 🔹 Приходи и разходи за категории тип "apartments"
            decimal totalExpenses = context.ApartmentTransactions
                .Where(t => apartments.Contains(t.ApartmentId) &&
                            t.Category.Kind != "Приход" &&
                            t.Category.Appliance == "apartments")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal totalIncomes = context.ApartmentTransactions
                .Where(t => apartments.Contains(t.ApartmentId) &&
                            t.Category.Kind == "Приход" &&
                            t.Category.Appliance == "apartments")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal remaining = totalExpenses - totalIncomes;

            // 🔹 Запис в таблицата TotalSum
            var totalSum = context.TotalSums.FirstOrDefault(ts => ts.EntranceId == entranceId);
            if (totalSum != null)
                totalSum.Summary = (int)remaining;
            else
                context.TotalSums.Add(new TotalSum
                {
                    EntranceId = entranceId,
                    Summary = (int)remaining
                });

            context.SaveChanges();
        }
 
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (var cat in _selectedIncomeCategories.Concat(_selectedExpenseCategories))
                UnsubscribeCategory(cat);
        }

        private void clearFieldsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in _selectedIncomeCategories)
            {
                cat.Amount = "0";
            }

            foreach (var cat in _selectedExpenseCategories)
            {
                cat.Amount = "0";
            }
        }
        public void UpdateDebtForApartment(int apartmentId)
        {
            using var context = new SyndiceoDBContext();

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == apartmentId);

            decimal totalExpenses = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind != "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal totalIncome = context.ApartmentTransactions
                .Where(t => t.ApartmentId == apartmentId && t.Category.Kind == "Приход")
                .Sum(t => (decimal?)t.Amount) ?? 0;

            if (debt == null)
            {
                debt = new Debt
                {
                    ApartmentId = apartmentId,
                    TotalSum = totalExpenses,
                    PaidSum = totalIncome
                };
                context.Debts.Add(debt);
            }
            else
            {
                debt.TotalSum = totalExpenses;
                debt.PaidSum = totalIncome;
            }

            context.SaveChanges();
        }

        private void removeCatButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверка дали изобщо е избрано нещо
            var selectedCategory = CategoryListBox.SelectedItem as Syndiceo.Data.Models.Category;

            if (selectedCategory == null)
            {
                MessageBox.Show("Моля, изберете категория от списъка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Потвърждение от потребителя (за сигурност)
            var result = MessageBox.Show($"Сигурни ли сте, че искате да изтриете категория '{selectedCategory.Name}'? " +
                                         "\nВсички свързани транзакции също ще бъдат изтрити!",
                                         "Потвърждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new SyndiceoDBContext())
                    {
                        // Намираме категорията в текущия контекст по ID
                        var categoryInDb = context.Categories.FirstOrDefault(c => c.Id == selectedCategory.Id);

                        if (categoryInDb != null)
                        {
                            // Изтриваме свързаните транзакции в апартаментите
                            var aptTransactions = context.ApartmentTransactions.Where(t => t.CategoryId == categoryInDb.Id);
                            context.ApartmentTransactions.RemoveRange(aptTransactions);

                            // Изтриваме свързаните транзакции във входовете
                            var entTransactions = context.EntranceTransactions.Where(t => t.CategoryId == categoryInDb.Id);
                            context.EntranceTransactions.RemoveRange(entTransactions);

                            // Изтриваме самата категория
                            context.Categories.Remove(categoryInDb);

                            // Запазваме промените
                            context.SaveChanges();

                            // Обновяваме UI
                            LoadCategories();
                            LoadExistingTransactions();

                            MessageBox.Show("Категорията беше премахната успешно.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Грешка при изтриване: {ex.Message}", "Критична грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        /*        public void ReapplyApartmentTransactions(int apartmentId)
                {
                    using var context = new SyndiceoDBContext();

                    var apartment = context.Apartments
                        .Include(a => a.ApartmentTransactions)
                        .FirstOrDefault(a => a.ApartmentId == apartmentId);

                    if (apartment == null)
                        return;

                    // ⚡ Взимаме копие на текущите транзакции,
                    // за да не модифицираме колекцията, докато я обхождаме
                    var oldTransactions = apartment.ApartmentTransactions.ToList();

                    foreach (var oldTransaction in oldTransactions)
                    {
                        var newTransaction = new ApartmentTransaction
                        {
                            ApartmentId = apartment.ApartmentId,
                            CategoryId = oldTransaction.CategoryId,
                            Amount = oldTransaction.Amount,
                            TransDate = DateOnly.FromDateTime(DateTime.Now)
                        };

                        context.ApartmentTransactions.Add(newTransaction);
                    }

                    context.SaveChanges();

                    // След това обновяваме сбора и дълговете
                    UpdateEntranceTotalSum(apartment.ApartmentId);
                    UpdateDebtForApartment(apartment.ApartmentId);
                }
        */

    }

    public class AmountChangedEventArgs : EventArgs
    {
        public decimal OldValue { get; }
        public decimal NewValue { get; }

        public AmountChangedEventArgs(decimal oldValue, decimal newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class CategoryViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }


        private string _amount = "0"; // Гарантира, че никога не е null
        private decimal _previousAmount = 0m;

        public string Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    decimal oldValue = GetDecimalAmount();

                    _amount = string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();

                    // Парсинг с InvariantCulture
                    if (!decimal.TryParse(_amount.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal newValue))
                        newValue = 0m;

                    _previousAmount = newValue;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
                    AmountChanged?.Invoke(this, new AmountChangedEventArgs(oldValue, newValue));
                }
            }
        }

        public decimal GetDecimalAmount()
        {
            // Никога не връща null
            if (string.IsNullOrWhiteSpace(_amount))
                return 0m;

            if (decimal.TryParse(_amount.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0m;
        }

        public event EventHandler<AmountChangedEventArgs> AmountChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void SetDecimalAmount(decimal value)
        {
            _amount = value.ToString("0.00");
            _previousAmount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
        }

        public void ClearAmountChangedSubscribers()
        {
            AmountChanged = null;
        }
    }
}
