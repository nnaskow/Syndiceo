using Syndiceo.Data.Models;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using Syndiceo.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Syndiceo.Windows
{
    public partial class TaxesWindow : Window //сърцето на проекта
    {
        private int? _apartmentId;
        private int? _entranceId;
        private List<Apartment> _allApartmentsInEntrance = new();
        private int _currentApartmentIndex = -1;
        private readonly ObservableCollection<CategoryViewModel> _selectedIncomeCategories = new();
        private readonly ObservableCollection<CategoryViewModel> _selectedExpenseCategories = new();
        private TaxesHelper _helper;

        public TaxesWindow(int? apartmentId = null, int? entranceId = null)
        {
            InitializeComponent();
            _apartmentId = apartmentId;
            _entranceId = entranceId;

            using var context = new SyndiceoDBContext();

            if (_apartmentId.HasValue)
            {
                var currentApt = context.Apartments.FirstOrDefault(a => a.ApartmentId == _apartmentId);
                if (currentApt != null) _entranceId = currentApt.EntranceId;

                cashboxLabel.Opacity = Cashbox.Opacity = 0.4;
                Cashbox.IsReadOnly = true;
            }

            if (_entranceId.HasValue)
            {
                _allApartmentsInEntrance = context.Apartments
                    .Where(a => a.EntranceId == _entranceId.Value)
                    .OrderBy(a => a.ApartmentNumber).ToList();

                _currentApartmentIndex = _allApartmentsInEntrance.FindIndex(a => a.ApartmentId == _apartmentId);

                var cb = context.Cashboxes.FirstOrDefault(c => c.EntranceId == _entranceId.Value);
                if (cb != null) Cashbox.Text = cb.CurrentBalance.ToString("N2");
            }

            IncomeCategoriesPanel.ItemsSource = _selectedIncomeCategories;
            ExpenseCategoriesPanel.ItemsSource = _selectedExpenseCategories;

            LoadExistingTransactions();
            UpdateNavigationUI();
        }


        private void SaveDataToDatabase()
        {
            var currentCats = _selectedIncomeCategories.Concat(_selectedExpenseCategories).ToList();
            using var context = new SyndiceoDBContext();

            if (_apartmentId.HasValue)
            {
                var dbTransactions = context.ApartmentTransactions
                    .Where(t => t.ApartmentId == _apartmentId.Value).ToList();

                foreach (var cat in currentCats)
                {
                    decimal amt = cat.GetDecimalAmount();
                    var existing = dbTransactions.FirstOrDefault(t => t.CategoryId == cat.Id);

                    if (existing != null) { existing.Amount = amt; context.ApartmentTransactions.Update(existing); }
                    else if (amt != 0) context.ApartmentTransactions.Add(new ApartmentTransaction { ApartmentId = _apartmentId.Value, CategoryId = cat.Id, Amount = amt, TransDate = DateOnly.FromDateTime(DateTime.Now) });
                }
                context.SaveChanges();
                UpdateCalculations(context, _apartmentId.Value);
            }
            else if (_entranceId.HasValue)
            {
                if (decimal.TryParse(Cashbox.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal balance))
                {
                    var cb = context.Cashboxes.FirstOrDefault(c => c.EntranceId == _entranceId.Value);
                    if (cb != null) { cb.CurrentBalance = balance; context.Cashboxes.Update(cb); }
                    else context.Cashboxes.Add(new Cashbox { EntranceId = _entranceId.Value, CurrentBalance = balance });
                }

                var dbEntranceTrans = context.EntranceTransactions
                    .Where(t => t.EntranceId == _entranceId.Value).ToList();

                foreach (var cat in currentCats)
                {
                    decimal amt = cat.GetDecimalAmount();
                    var existing = dbEntranceTrans.FirstOrDefault(t => t.CategoryId == cat.Id);

                    if (existing != null) { existing.Amount = amt; context.EntranceTransactions.Update(existing); }
                    else if (amt != 0) context.EntranceTransactions.Add(new EntranceTransaction { EntranceId = _entranceId.Value, CategoryId = cat.Id, Amount = amt, TransDate = DateOnly.FromDateTime(DateTime.Now) });
                }
                context.SaveChanges();
            }
        }

        private void LoadExistingTransactions()
        {
            using var context = new SyndiceoDBContext();

            _selectedIncomeCategories.Clear();
            _selectedExpenseCategories.Clear();

            if (_apartmentId.HasValue)
            {
                var trans = context.ApartmentTransactions
                    .Where(t => t.ApartmentId == _apartmentId.Value)
                    .Select(t => new { t.CategoryId, t.Amount, t.Category.Name, t.Category.Kind }).ToList();

                foreach (var t in trans)
                {
                    var vm = new CategoryViewModel { Id = t.CategoryId, Name = t.Name, Amount = t.Amount.ToString("N2") };
                    if (t.Kind == "Приход") _selectedIncomeCategories.Add(vm); else _selectedExpenseCategories.Add(vm);
                }
            }
            else if (_entranceId.HasValue)
            {
                var trans = context.EntranceTransactions
                    .Where(t => t.EntranceId == _entranceId.Value)
                    .Select(t => new { t.CategoryId, t.Amount, t.Category.Name, t.Category.Kind }).ToList();

                foreach (var t in trans)
                {
                    var vm = new CategoryViewModel { Id = t.CategoryId, Name = t.Name, Amount = t.Amount.ToString("N2") };
                    if (t.Kind == "Приход") _selectedIncomeCategories.Add(vm); else _selectedExpenseCategories.Add(vm);
                }
            }
        }
        private void SaveOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToDatabase();

            var originalContent = SaveOnlyButton.Content;
            SaveOnlyButton.Content = "ЗАПАЗЕНО";
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            timer.Tick += (s, args) => { SaveOnlyButton.Content = originalContent; timer.Stop(); };
            timer.Start();
        }

        private void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToDatabase();
            this.Close();
        }


        private void SwitchToApartment(int index)
        {
            if (index < 0 || index >= _allApartmentsInEntrance.Count) return;

            SaveDataToDatabase();

            _currentApartmentIndex = index;
            _apartmentId = _allApartmentsInEntrance[_currentApartmentIndex].ApartmentId;

            foreach (var cat in _selectedIncomeCategories.Concat(_selectedExpenseCategories))
                UnsubscribeCategory(cat);

            _selectedIncomeCategories.Clear();
            _selectedExpenseCategories.Clear();

            LoadExistingTransactions();
            UpdateNavigationUI();
        }

        private void UpdateNavigationUI()
        {
            if (_currentApartmentIndex >= 0)
            {
                var current = _allApartmentsInEntrance[_currentApartmentIndex];
                CurrentApartmentTextBlock.Text = $"№ {current.ApartmentNumber}";
                ApartmentSearchTextBox.Text = current.ApartmentNumber.ToString();
            }
            RefreshCategoryList();
        }

        private void RefreshCategoryList()
        {
            using var context = new SyndiceoDBContext();
            string type = _apartmentId.HasValue ? "apartments" : "entrances";
            var selectedIds = _selectedIncomeCategories.Select(x => x.Id).Concat(_selectedExpenseCategories.Select(x => x.Id)).ToHashSet();
            CategoryListBox.ItemsSource = context.Categories
                .Where(c => c.Appliance == type && !selectedIds.Contains(c.Id))
                .OrderBy(c => c.Name).ToList();
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryTextBox.Text)) return;
            using var context = new SyndiceoDBContext();
            var newCat = new Category
            {
                Name = CategoryTextBox.Text.Trim(),
                Kind = (CategoryKindComboBox.SelectedItem as ComboBoxItem).Content.ToString(),
                Appliance = _apartmentId.HasValue ? "apartments" : "entrances"
            };
            context.Categories.Add(newCat);
            context.SaveChanges();
            CategoryTextBox.Clear();
            RefreshCategoryList();
        }

        private void AddSelected()
        {
            if (CategoryListBox.SelectedItem is Category cat)
            {
                var vm = new CategoryViewModel { Id = cat.Id, Name = cat.Name, Amount = "0" };
                if (_apartmentId.HasValue && !Properties.Settings.Default.taxesWdHelpNeeded) SubscribeCategory(vm, _apartmentId.Value);
                if (cat.Kind == "Приход") _selectedIncomeCategories.Add(vm); else _selectedExpenseCategories.Add(vm);
                RefreshCategoryList();
            }
        }

        private void RemoveCategory(object sender)
        {
            if (sender is Button btn && btn.DataContext is CategoryViewModel cat)
            {
                using var context = new SyndiceoDBContext();
                if (_apartmentId.HasValue)
                {
                    var dbTrans = context.ApartmentTransactions
                        .FirstOrDefault(t => t.ApartmentId == _apartmentId.Value && t.CategoryId == cat.Id);

                    if (dbTrans != null)
                    {
                        context.ApartmentTransactions.Remove(dbTrans);
                        context.SaveChanges();
                        UpdateCalculations(context, _apartmentId.Value);
                    }
                }
                else if (_entranceId.HasValue)
                {
                    var dbEntranceTrans = context.EntranceTransactions
                        .FirstOrDefault(t => t.EntranceId == _entranceId.Value && t.CategoryId == cat.Id);

                    if (dbEntranceTrans != null)
                    {
                        context.EntranceTransactions.Remove(dbEntranceTrans);
                        context.SaveChanges();
                    }
                }

                _selectedIncomeCategories.Remove(cat);
                _selectedExpenseCategories.Remove(cat);

                RefreshCategoryList();
            }
        }


        private void UpdateCalculations(SyndiceoDBContext context, int apartmentId)
        {
            var apt = context.Apartments.Include(a => a.Entrance).FirstOrDefault(a => a.ApartmentId == apartmentId);
            if (apt == null) return;
            var aptIds = context.Apartments.Where(a => a.EntranceId == apt.EntranceId).Select(a => a.ApartmentId).ToList();
            var allTrans = context.ApartmentTransactions.Where(t => aptIds.Contains(t.ApartmentId)).Include(t => t.Category).ToList();

            decimal totalExp = allTrans.Where(t => t.Category.Kind != "Приход").Sum(t => t.Amount);
            decimal totalInc = allTrans.Where(t => t.Category.Kind == "Приход").Sum(t => t.Amount);

            var ts = context.TotalSums.FirstOrDefault(x => x.EntranceId == apt.EntranceId);
            if (ts != null) ts.Summary = (int)(totalExp - totalInc);
            else context.TotalSums.Add(new TotalSum { EntranceId = apt.EntranceId, Summary = (int)(totalExp - totalInc) });

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == apartmentId);
            decimal aExp = allTrans.Where(t => t.ApartmentId == apartmentId && t.Category.Kind != "Приход").Sum(t => t.Amount);
            decimal aInc = allTrans.Where(t => t.ApartmentId == apartmentId && t.Category.Kind == "Приход").Sum(t => t.Amount);

            if (debt != null) { debt.TotalSum = aExp; debt.PaidSum = aInc; }
            else context.Debts.Add(new Debt { ApartmentId = apartmentId, TotalSum = aExp, PaidSum = aInc });

            context.SaveChanges();
        }

        private void PrevApartmentButton_Click(object sender, RoutedEventArgs e) => SwitchToApartment(_currentApartmentIndex - 1);
        private void NextApartmentButton_Click(object sender, RoutedEventArgs e) => SwitchToApartment(_currentApartmentIndex + 1);
        private void ApartmentSearchTextBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { int idx = _allApartmentsInEntrance.FindIndex(a => a.ApartmentNumber.ToString() == ApartmentSearchTextBox.Text.Trim()); if (idx >= 0) SwitchToApartment(idx); else MessageBox.Show("Няма такъв апартамент!"); } }
        private void addCatButton_Click(object sender, RoutedEventArgs e) => AddSelected();
        private void CategoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AddSelected();
        private void RemoveIncomeCategory_Click(object sender, RoutedEventArgs e) => RemoveCategory(sender);
        private void RemoveExpenseCategory_Click(object sender, RoutedEventArgs e) => RemoveCategory(sender);
        private void CategoryTextBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddCategoryButton_Click(sender, e); }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_apartmentId.HasValue)
            {
                using var db = new SyndiceoDBContext();
                var apt = db.Apartments.FirstOrDefault(a => a.ApartmentId == _apartmentId.Value);

                this.Title = "Управление на такси за апартамент";
            }
            else if (_entranceId.HasValue)
            {
                ApartmentNavigation.Visibility = Visibility.Collapsed;
                this.Title = "Управление на такси за вход";
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e) { foreach (var cat in _selectedIncomeCategories.Concat(_selectedExpenseCategories)) UnsubscribeCategory(cat); }
        private void clearFieldsButton_Click(object sender, RoutedEventArgs e) { foreach (var cat in _selectedIncomeCategories.Concat(_selectedExpenseCategories)) cat.Amount = "0"; }

        private void SubscribeCategory(CategoryViewModel cat, int aptId) => cat.AmountChanged += (s, e) => _helper?.AdjustRemainingAmount(cat.Id, e.NewValue - e.OldValue);
        private void UnsubscribeCategory(CategoryViewModel cat) => cat.ClearAmountChangedSubscribers();

        private void removeCatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is Category selected)
            {
                var result = MessageBox.Show(
                    $"ВНИМАНИЕ: Изтриването на категория '{selected.Name}' ще премахне и всички финансови записи, свързани с нея в този вход/апартамент!\n\nСигурни ли сте?",
                    "Потвърждение за изтриване",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new SyndiceoDBContext();

                        var dbCat = context.Categories.Find(selected.Id);
                        if (dbCat != null)
                        {
                            var aptTrans = context.ApartmentTransactions.Where(t => t.CategoryId == selected.Id);
                            context.ApartmentTransactions.RemoveRange(aptTrans);

                            var entranceTrans = context.EntranceTransactions.Where(t => t.CategoryId == selected.Id);
                            context.EntranceTransactions.RemoveRange(entranceTrans);

                            context.Categories.Remove(dbCat);

                            context.SaveChanges();

                            RefreshCategoryList();

                            var inIncome = _selectedIncomeCategories.FirstOrDefault(x => x.Id == selected.Id);
                            if (inIncome != null) _selectedIncomeCategories.Remove(inIncome);

                            var inExpense = _selectedExpenseCategories.FirstOrDefault(x => x.Id == selected.Id);
                            if (inExpense != null) _selectedExpenseCategories.Remove(inExpense);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Грешка при изтриване: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Моля, избери категория от списъка!", "Информация");
            }
        }
    }


    public class AmountChangedEventArgs : EventArgs
    {
        public decimal OldValue { get; }
        public decimal NewValue { get; }
        public AmountChangedEventArgs(decimal oldV, decimal newV) { OldValue = oldV; NewValue = newV; }
    }

    public class CategoryViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        private string _amount = "0";
        public event EventHandler<AmountChangedEventArgs> AmountChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    decimal oldV = GetDecimalAmount();
                    _amount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
                    AmountChanged?.Invoke(this, new AmountChangedEventArgs(oldV, GetDecimalAmount()));
                }
            }
        }

        public decimal GetDecimalAmount() => decimal.TryParse(_amount.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res) ? res : 0m;
        public void ClearAmountChangedSubscribers() => AmountChanged = null;
    }
}