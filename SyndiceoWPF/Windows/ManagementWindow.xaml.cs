using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Models;
using Syndiceo.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Block = Syndiceo.Data.Models.Block;
using Syndiceo.Data.Models;

namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for ManagementWindow.xaml
    /// </summary>
    public partial class ManagementWindow : Window
    {
        DispatcherTimer greetingTimer;
        private string notesFilePath;

          public ManagementWindow()
        {
            InitializeComponent();
            UpdateGreeting();

            DispatcherTimer greetingTimer = new DispatcherTimer();
            greetingTimer.Interval = TimeSpan.FromMinutes(5);
            greetingTimer.Tick += (s, e) => UpdateGreeting();
            greetingTimer.Start();

            DispatcherTimer clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) =>
            {
                Clock.Text = $"🕓 Часът в момента е: {DateTime.Now:HH:mm:ss}";
            };
            clockTimer.Start();

            string appDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Syndiceo");

            if (!Directory.Exists(appDataFolder))
                Directory.CreateDirectory(appDataFolder);

            notesFilePath = System.IO.Path.Combine(appDataFolder, "Notes.txt");
            LoadEntrances();
            EntrancesDataGrid.ItemsSource = Entrances;
            ApartmentsDataGrid.ItemsSource = Apartments;
            BlocksDataGrid.ItemsSource = Blocks;
            AddressesDataGrid.ItemsSource = Addresses;
            LoadNotes();
          this.Loaded += Window_Loaded;
        }
        private void LoadNotes()
        {
            if (File.Exists(notesFilePath))
            {
                NotesTextBox.Text = File.ReadAllText(notesFilePath);
                NotesTextBox.FontStyle = FontStyles.Normal;
            }
        }

        private void SaveNotes()
        {
            try
            {
                File.WriteAllText(notesFilePath, NotesTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при запазване: {ex.Message}");
            }
        }

        private void UpdateGreeting()
        {
            string greeting;
            string user;
            string extraMessage;
            using (var context = new SyndiceoDBContext())
            {
                var username = context.Logins
                       .Select(u => u.PersonName)
                       .FirstOrDefault();

                user = username ?? "Потребител";
            }

            int hour = DateTime.Now.Hour;
            Random rnd = new Random();

            if (hour >= 5 && hour < 12) // Сутрин
            {
                greeting = "Добро утро";
                string[] morningMessages = {
        "Ставай, че пак няма асансьор",
        "Кафето е готово, ама съседите мрънкат.",
        "Проверка на ключовете преди да излезеш.",
        "Въздухът в коридора е студен, не се мотаеш.",
        "Съседите вече мрънкат за шум.",
        "Ставай, пощата няма да се отвори сама.",
        "Добро утро! Провери светлините на входа.",
        "Асфалтът е мокър, обувките ще мръднат.",
        "Входът чака твоите крачки.",
        "Ставай, иначе котката ще се изгуби.",
        "Как минава денят ти до сега?",
        "Събуди ли се добре?",
        "Имаш ли време за едно кафе преди обхода?",
        "Как се чувстваш тази сутрин?",
        "Готов ли си за новия ден?",
        "Какви планове имаш днес?",
        "Всичко наред ли е вкъщи?",
        "Има ли нещо, което те притеснява?",
        "Как се чувства котката/домът тази сутрин?",
        "Готов ли си за първото си усмихване на блока?"
    };
                extraMessage = WrapText(morningMessages[rnd.Next(morningMessages.Length)]);

            }
            else if (hour >= 12 && hour < 18) // Следобед
            {
                greeting = "Добър ден";
                string[] dayMessages = {
        "Следобед е, пощата чака твоите ръце.",
        "Коридорът пак има нужда от внимание.",
        "Съседът слуша музика, усмихни се или мрънкай.",
        "Сметките няма да се платят сами.",
        "Време за малък обход на входа.",
        "Проверка на асансьора – както казва баба ми.",
        "Добър ден! Светлините пак мигат.",
        "Котката е гладна, но коридорът е приоритет.",
        "Стълбите не се почистват сами.",
        "Входната врата се нуждае от твоята усмивка.",
        "Как протича денят ти?",
        "Имаш ли почивка между обходите?",
        "Как се чувстваш към момента?",
        "Доволен ли си от сутрешните си задачи?",
        "Има ли нещо, което те радва днес?",
        "Какво те чака в следващите часове?",
        "Направи ли нещо интересно до сега?",
        "Имаш ли нужда от кратка разходка?",
        "Как се чувстваш физически и психически?",
        "Ще има ли време за малка пауза днес?"
    };
                extraMessage = WrapText(dayMessages[rnd.Next(dayMessages.Length)]);

            }
            else if (hour >= 18 && hour < 22) // Вечер
            {
                greeting = "Добър вечер";
                string[] eveningMessages = {
        "Входът е тих, можеш да се отпуснеш.",
        "Проверка на светлините преди съседите да спят.",
        "Съседът вече е у дома, здравей с усмивка.",
        "Пощата е празна, какво друго остава?",
        "Асфалтът вече изстива, блокът е спокоен.",
        "Време е да провериш асансьора.",
        "Вечерта е твоя, но блокът също има нужди.",
        "Не забравяй ключовете, дори вечер.",
        "Котката спи, стълбите са тихи.",
        "Входът е спокоен, усмихни се.",
        "Как мина денят ти?",
        "Имаш ли време за почивка вечерта?",
        "Доволен ли си от постигнатото днес?",
        "Как се чувстваш след работа/учене?",
        "Имаш ли план за вечерта?",
        "Смяташ ли да се разходиш малко?",
        "Какво беше най-хубавото днес?",
        "Имаш ли нужда от малко релакс?",
        "Доволен ли си от срещите и задачите днес?",
        "Ще отделиш ли време за хоби тази вечер?"
    };
                extraMessage = WrapText(eveningMessages[rnd.Next(eveningMessages.Length)]);
            }
            else // Нощ
            {
                greeting = "Лека нощ";
                string[] nightMessages = {
        "Входът е тих, можеш да си починеш.",
        "Асфалтът замръзва, ти не бързай.",
        "Съседът спи, блокът също.",
        "Пощата няма да се отвори до утре.",
        "Ключовете са на сигурно място, надявам се.",
        "Добра нощ! Провери светлините.",
        "Котката спи, но се огледай за нея.",
        "Стълбите са празни, ползвай момента.",
        "Вратата е заключена, усмихни се.",
        "Блокът чака нов ден, ти – сън.",
        "Как мина денят ти?",
        "Беше ли успешен денят за теб?",
        "Имаш ли нужда от спокойна вечер?",
        "Как се чувстваш преди сън?",
        "Има ли нещо, което искаш да мислиш преди заспиване?",
        "Сънуваш ли хубави неща?",
        "Доволен ли си от днешните си усилия?",
        "Има ли нещо, което те притеснява преди сън?",
        "Готов ли си за новия ден утре?",
        "Лягаш ли с усмивка или с мисли?"
    };
                extraMessage = WrapText(nightMessages[rnd.Next(nightMessages.Length)]);
            }
            if(user.Length >= 13)
                user = WrapText(user, 13);

            welcomeLabel.Text = $"{greeting}, {user}!{Environment.NewLine}{extraMessage}";
        }
        private string WrapText(string text, int maxLineLength = 27)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(' ');
            var line = "";
            var result = new List<string>();

            foreach (var word in words)
            {
                if ((line + word).Length > maxLineLength)
                {
                    result.Add(line.TrimEnd());
                    line = "";
                }
                line += word + " ";
            }

            if (line.Length > 0)
                result.Add(line.TrimEnd());

            return string.Join(Environment.NewLine, result);
        }

        private void UpdateBreadcrumbs()
        {
            BreadcrumbAddresses.Visibility = Visibility.Hidden;
            BreadcrumbBlocks.Visibility = Visibility.Hidden;
            BreadcrumbEntrances.Visibility = Visibility.Hidden;
            BreadcrumbApartments.Visibility = Visibility.Hidden;

            arr1.Visibility = Visibility.Hidden;
            arr2.Visibility = Visibility.Hidden;
            arr3.Visibility = Visibility.Hidden;

            if (AddressesDataGrid.Visibility == Visibility.Visible)
            {
                BreadcrumbAddresses.Visibility = Visibility.Visible;
            }
            else if (BlocksDataGrid.Visibility == Visibility.Visible)
            {
                BreadcrumbAddresses.Visibility = Visibility.Visible;
                BreadcrumbBlocks.Visibility = Visibility.Visible;
                arr1.Visibility = Visibility.Visible;
            }
            else if (EntrancesDataGrid.Visibility == Visibility.Visible)
            {
                BreadcrumbAddresses.Visibility = Visibility.Visible;
                BreadcrumbBlocks.Visibility = Visibility.Visible;
                BreadcrumbEntrances.Visibility = Visibility.Visible;

                arr1.Visibility = Visibility.Visible;
                arr2.Visibility = Visibility.Visible;
            }
            else if (ApartmentsDataGrid.Visibility == Visibility.Visible)
            {
                BreadcrumbAddresses.Visibility = Visibility.Visible;
                BreadcrumbBlocks.Visibility = Visibility.Visible;
                BreadcrumbEntrances.Visibility = Visibility.Visible;
                BreadcrumbApartments.Visibility = Visibility.Visible;

                arr1.Visibility = Visibility.Visible;
                arr2.Visibility = Visibility.Visible;
                arr3.Visibility = Visibility.Visible;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            UpdateBreadcrumbs();
            CleanOldBackups();
            searchTxtBox.TextChanged += searchTxtBox_TextChanged;
            Properties.Settings.Default.MainWindowClosing = false;
            Properties.Settings.Default.isReportDone = false;
            Properties.Settings.Default.areThereAnyLastPayments = false;
            ApplyPaymentMarkingToApartments();
            if (Enum.TryParse(Properties.Settings.Default.WindowState, out WindowState state))
            {
                this.WindowState = state;
            }
        }
        public void CleanOldBackups()
        {
            string backupFolder = System.IO.Path.Combine(
     Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
     "Syndiceo",
     "Backups"
 );

            if (!Directory.Exists(backupFolder))
                return;

            var backupFiles = Directory.GetFiles(backupFolder, "*.bak");

            foreach (var file in backupFiles)
            {
                try
                {
                    DateTime creationDate = File.GetCreationTime(file);
                    if ((DateTime.Now - creationDate).TotalDays > 14)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    string logFile = System.IO.Path.Combine(backupFolder, "BackupErrors.log");
                    File.AppendAllText(logFile, DateTime.Now + " - Неуспешно изтриване на " + file + ": " + ex.Message + Environment.NewLine);
                }
            }

            var recentFiles = Directory.GetFiles(backupFolder, "*.bak")
                                       .Select(f => new FileInfo(f))
                                       .Where(f => (DateTime.Now - f.CreationTime).TotalDays <= 7)
                                       .OrderBy(f => f.CreationTime)
                                       .ToList();

            if (recentFiles.Count > 30)
            {
                var filesToDelete = recentFiles.Take(15);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        string logFile = System.IO.Path.Combine(backupFolder, "BackupErrors.log");
                        File.AppendAllText(logFile, DateTime.Now + " - Неуспешно изтриване на " + file.FullName + ": " + ex.Message + Environment.NewLine);
                    }
                }
            }

        }

        private AddToDBWindow _openAddWindow;
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_openAddWindow != null && _openAddWindow.IsVisible)
                {
                    MessageBox.Show("Прозорецът за добавяне вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    _openAddWindow.Focus();
                    return;
                }

                _openAddWindow = new AddToDBWindow();

                _openAddWindow.Closed += (s, args) => _openAddWindow = null;

                _openAddWindow.ShowDialog();

                LoadData();
                UpdateBreadcrumbs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при отваряне на прозореца за добавяне: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private EditWindow _openEditWindow;

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_openEditWindow != null && _openEditWindow.IsVisible)
                {
                    MessageBox.Show("Прозорецът за редакция вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    _openEditWindow.Focus();
                    return;
                }

                AddressViewModel selectedAddressVm = null;
                BlockViewModel selectedBlockVm = null;
                EntranceViewModel selectedEntranceVm = null;
                ApartmentViewModel selectedApartmentVm = null;

                using (var context = new SyndiceoDBContext())
                {
                    if (AddressesDataGrid.Visibility == Visibility.Visible && AddressesDataGrid.SelectedItem is AddressViewModel addrVm)
                    {
                        var addressEntity = context.Addresses
                                                   .FirstOrDefault(a => a.AddressId == addrVm.Id);
                        if (addressEntity != null)
                        {
                            selectedAddressVm = new AddressViewModel
                            {
                                Id = addressEntity.AddressId,
                                Street = addressEntity.Street
                            };
                        }
                    }
                    else if (BlocksDataGrid.Visibility == Visibility.Visible && BlocksDataGrid.SelectedItem is BlockViewModel blkVm)
                    {
                        var blockEntity = context.Blocks
                                                 .Include(b => b.Address)
                                                 .FirstOrDefault(b => b.BlockId == blkVm.Id);

                        if (blockEntity != null)
                        {
                            selectedBlockVm = new BlockViewModel
                            {
                                Id = blockEntity.BlockId,
                                BlockName = blockEntity.BlockName,
                                Address = new AddressViewModel
                                {
                                    Id = blockEntity.Address.AddressId,
                                    Street = blockEntity.Address.Street
                                }
                            };
                            selectedAddressVm = selectedBlockVm.Address;
                        }
                    }
                    else if (EntrancesDataGrid.Visibility == Visibility.Visible && EntrancesDataGrid.SelectedItem is EntranceViewModel entVm)
                    {
                        var entranceEntity = context.Entrances
                                                    .Include(e => e.Block)
                                                        .ThenInclude(b => b.Address)
                                                    .FirstOrDefault(e => e.EntranceId == entVm.Id);

                        if (entranceEntity != null)
                        {
                            selectedEntranceVm = new EntranceViewModel
                            {
                                Id = entranceEntity.EntranceId,
                                Name = entranceEntity.EntranceName,
                                Block = new BlockViewModel
                                {
                                    Id = entranceEntity.Block.BlockId,
                                    BlockName = entranceEntity.Block.BlockName,
                                    Address = new AddressViewModel
                                    {
                                        Id = entranceEntity.Block.Address.AddressId,
                                        Street = entranceEntity.Block.Address.Street
                                    }
                                }
                            };
                            selectedBlockVm = selectedEntranceVm.Block;
                            selectedAddressVm = selectedBlockVm.Address;
                        }
                    }
                    else if (ApartmentsDataGrid.Visibility == Visibility.Visible && ApartmentsDataGrid.SelectedItem is ApartmentViewModel aptVm)
                    {
                        var apartmentEntity = context.Apartments
                            .Include(a => a.Entrance)
                                .ThenInclude(e => e.Block)
                                    .ThenInclude(b => b.Address)
                            .Include(a => a.Owners)
                            .FirstOrDefault(a => a.ApartmentId == aptVm.ApartmentId);

                        if (apartmentEntity != null)
                        {
                            var owner = apartmentEntity.Owners.FirstOrDefault();
                            selectedApartmentVm = new ApartmentViewModel
                            {
                                ApartmentId = apartmentEntity.ApartmentId,
                                ApartmentNumber = apartmentEntity.ApartmentNumber,
                                Note = apartmentEntity.Note,
                                ResidentCount = apartmentEntity.ResidentCount ?? 0,
                                OwnerName = owner?.OwnerName,
                                OwnerPhone = owner?.PhoneNumber,
                                Entrance = apartmentEntity.Entrance != null ? new EntranceViewModel
                                {
                                    Id = apartmentEntity.Entrance.EntranceId,
                                    Name = apartmentEntity.Entrance.EntranceName,
                                    Block = apartmentEntity.Entrance.Block != null ? new BlockViewModel
                                    {
                                        Id = apartmentEntity.Entrance.Block.BlockId,
                                        BlockName = apartmentEntity.Entrance.Block.BlockName,
                                        Address = apartmentEntity.Entrance.Block.Address != null ? new AddressViewModel
                                        {
                                            Id = apartmentEntity.Entrance.Block.Address.AddressId,
                                            Street = apartmentEntity.Entrance.Block.Address.Street
                                        } : null
                                    } : null
                                } : null
                            };

                            selectedEntranceVm = selectedApartmentVm.Entrance;
                            selectedBlockVm = selectedEntranceVm?.Block;
                            selectedAddressVm = selectedBlockVm?.Address;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Моля, изберете елемент за редакция.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                _openEditWindow = new EditWindow(
                    address: selectedAddressVm,
                    block: selectedBlockVm,
                    entrance: selectedEntranceVm,
                    apartment: selectedApartmentVm
                );

                _openEditWindow.Closed += (s, args) => _openEditWindow = null;

                _openEditWindow.ShowDialog();

                LoadData();
                UpdateBreadcrumbs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при отваряне на редакцията: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                object selectedItem = null;
                string itemType = null;

                if (AddressesDataGrid.Visibility == Visibility.Visible)
                {
                    selectedItem = AddressesDataGrid.SelectedItem as AddressViewModel;
                    itemType = "адрес";
                }
                else if (BlocksDataGrid.Visibility == Visibility.Visible)
                {
                    selectedItem = BlocksDataGrid.SelectedItem as BlockViewModel;
                    itemType = "блок";
                }
                else if (EntrancesDataGrid.Visibility == Visibility.Visible)
                {
                    selectedItem = EntrancesDataGrid.SelectedItem as EntranceViewModel;
                    itemType = "вход";
                }
                else if (ApartmentsDataGrid.Visibility == Visibility.Visible)
                {
                    selectedItem = ApartmentsDataGrid.SelectedItem as ApartmentViewModel;
                    itemType = "апартамент";
                }

                if (selectedItem == null)
                {
                    MessageBox.Show("Моля, изберете елемент от списъка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirmWindow = new DeleteConfirmationWindow { Owner = this };
                if (confirmWindow.ShowDialog() != true)
                    return;

                bool deleteRelated = confirmWindow.DeleteRelated;

                using var context = new SyndiceoDBContext();

                if (itemType == "адрес" && selectedItem is AddressViewModel addressVm)
                {
                    var entity = context.Addresses
                                        .Include(a => a.Blocks)
                                            .ThenInclude(b => b.Entrances)
                                        .FirstOrDefault(a => a.AddressId == addressVm.Id);

                    if (entity != null)
                    {
                        if (entity.Blocks.Any() && !deleteRelated)
                        {
                            MessageBox.Show("Има свързани блокове. Нужно е допълнително разрешение за изтриване.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (deleteRelated)
                        {
                            foreach (var block in entity.Blocks.ToList())
                                DeleteBlockWithChildren(block, context);
                        }

                        context.Addresses.Remove(entity);
                        context.SaveChanges();
                        LoadData();
                    }
                }

                else if (itemType == "блок" && selectedItem is BlockViewModel blockVm)
                {
                    var entity = context.Blocks
                                        .Include(b => b.Entrances)
                                            .ThenInclude(e => e.Apartments)
                                        .FirstOrDefault(b => b.BlockId == blockVm.Id);

                    if (entity != null)
                    {
                        if (entity.Entrances.Any() && !deleteRelated)
                        {
                            MessageBox.Show("Има свързани входове. Нужно е допълнително разрешение за изтриване.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (deleteRelated)
                        {
                            foreach (var entrance in entity.Entrances.ToList())
                                DeleteEntranceWithChildren(entrance, context);
                        }

                        context.Blocks.Remove(entity);
                        context.SaveChanges();
                        LoadData();
                    }
                }

                else if (itemType == "вход" && selectedItem is EntranceViewModel entranceVm)
                {
                    var entity = context.Entrances
                                        .Include(e => e.Apartments)
                                            .ThenInclude(a => a.Owners)
                                        .Include(e => e.Apartments)
                                            .ThenInclude(a => a.ApartmentTransactions)
                                        .Include(e => e.EntranceTransactions)
                                        .FirstOrDefault(e => e.EntranceId == entranceVm.Id);

                    if (entity != null)
                    {
                        if ((entity.Apartments.Any() || entity.EntranceTransactions.Any()) && !deleteRelated)
                        {
                            MessageBox.Show("Има свързани апартаменти или транзакции. Нужно е допълнително разрешение за изтриване.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        DeleteEntranceWithChildren(entity, context);
                        context.SaveChanges();
                        LoadData();
                    }
                }

                else if (itemType == "апартамент" && selectedItem is ApartmentViewModel apartmentVm)
                {
                    var entity = context.Apartments
                                        .Include(a => a.Owners)
                                        .Include(a => a.ApartmentTransactions)
                                        .FirstOrDefault(a => a.ApartmentId == apartmentVm.ApartmentId);

                    if (entity != null)
                    {
                        if ((entity.Owners.Any() || entity.ApartmentTransactions.Any()) && !deleteRelated)
                        {
                            MessageBox.Show("Има собственици или транзакции. Нужно е допълнително разрешение за изтриване.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        DeleteApartmentWithChildren(entity, context);
                        context.SaveChanges();
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при изтриване: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBlockWithChildren(Block block, SyndiceoDBContext context)
        {
            context.Entry(block).Collection(b => b.Entrances).Load();
            foreach (var entrance in block.Entrances.ToList())
                DeleteEntranceWithChildren(entrance, context);

            context.Blocks.Remove(block);
        }

        private void DeleteEntranceWithChildren(Entrance entrance, SyndiceoDBContext context)
        {
            context.Entry(entrance).Collection(e => e.EntranceTransactions).Load();
            context.Entry(entrance).Collection(e => e.Apartments).Load();

            foreach (var tr in entrance.EntranceTransactions.ToList())
                context.EntranceTransactions.Remove(tr);

            foreach (var apt in entrance.Apartments.ToList())
                DeleteApartmentWithChildren(apt, context);

            context.Entrances.Remove(entrance);
        }

        private void DeleteApartmentWithChildren(Apartment apartment, SyndiceoDBContext context)
        {
            context.Entry(apartment).Collection(a => a.Owners).Load();
            context.Entry(apartment).Collection(a => a.ApartmentTransactions).Load();

            foreach (var o in apartment.Owners.ToList())
                context.Owners.Remove(o);

            foreach (var t in apartment.ApartmentTransactions.ToList())
                context.ApartmentTransactions.Remove(t);

            context.Apartments.Remove(apartment);
        }
        private void RefershButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private void ShowWatermark()
        {
            WatermarkLayer.Visibility = Visibility.Visible;

            if (WatermarkRotate != null)
            {
                this.RegisterName("WatermarkRotate", WatermarkRotate);

                var storyboard = (Storyboard)this.Resources["WatermarkRotateStoryboard"] as Storyboard;
                if (storyboard != null)
                {
                    Storyboard.SetTargetName(storyboard.Children[0], "WatermarkRotate");
                    Storyboard.SetTargetProperty(storyboard.Children[0], new PropertyPath("Angle"));

                    storyboard.Begin(this, true);
                }
            }

        }
        private void UpdateBreadcrumbText(object selectedItem)
        {
            string address = "";
            string block = "";
            string entrance = "";
            string apartment = "";

            using var db = new SyndiceoDBContext();

            switch (selectedItem)
            {
                case ApartmentViewModel aVM:
                    var apartmentEntity = db.Apartments
                        .Include(a => a.Entrance)
                            .ThenInclude(e => e.Block)
                                .ThenInclude(b => b.Address)
                        .FirstOrDefault(a => a.ApartmentId == aVM.ApartmentId);

                    if (apartmentEntity != null)
                    {
                        apartment = apartmentEntity.ApartmentNumber.ToString();
                        entrance = apartmentEntity.Entrance?.EntranceName ?? "";
                        block = apartmentEntity.Entrance?.Block?.BlockName ?? "";
                        address = apartmentEntity.Entrance?.Block?.Address?.Street ?? "";
                    }
                    break;

                case EntranceViewModel eVM:
                    var entranceEntity = db.Entrances
                        .Include(e => e.Block)
                            .ThenInclude(b => b.Address)
                        .FirstOrDefault(e => e.EntranceId == eVM.Id);

                    if (entranceEntity != null)
                    {
                        entrance = entranceEntity.EntranceName;
                        block = entranceEntity.Block?.BlockName ?? "";
                        address = entranceEntity.Block?.Address?.Street ?? "";
                    }
                    break;

                case BlockViewModel bVM:
                    var blockEntity = db.Blocks
                        .Include(b => b.Address)
                        .FirstOrDefault(b => b.BlockId == bVM.Id);

                    if (blockEntity != null)
                    {
                        block = blockEntity.BlockName;
                        address = blockEntity.Address?.Street ?? "";
                    }
                    break;


                case AddressViewModel adVM:
                    var addressEntity = db.Addresses
                        .FirstOrDefault(a => a.AddressId == adVM.Id);

                    if (addressEntity != null)
                    {
                        address = addressEntity.Street;
                    }
                    break;

            }

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address)) parts.Add($"Адрес: {address}");
            if (!string.IsNullOrEmpty(block)) parts.Add(block);
            if (!string.IsNullOrEmpty(entrance)) parts.Add($"вх.{entrance}");
            if (!string.IsNullOrEmpty(apartment)) parts.Add($"апт: {apartment}");

            BreadCrumbText.Text = "* " + string.Join(", ", parts) + " *";
        }

        private async void HideWatermark(int delayTime)
        {
            var storyboard = (Storyboard)this.Resources["WatermarkRotateStoryboard"] as Storyboard;
            if (storyboard != null)
            {
                await Task.Delay(delayTime);

                storyboard.Stop(this);
            }

            WatermarkLayer.Visibility = Visibility.Collapsed;
        }

        private void LoadData()
        {
            if (WatermarkRotate != null)
            {
                ShowWatermark();
                HideWatermark(800);
            }

            using var db = new SyndiceoDBContext();

            var selectedAddress = AddressesDataGrid.SelectedItem as AddressViewModel;
            var selectedBlock = BlocksDataGrid.SelectedItem as BlockViewModel;
            var selectedEntrance = EntrancesDataGrid.SelectedItem as EntranceViewModel;

            // ---------- Addresses ----------
            if (AddressesDataGrid.Visibility == Visibility.Visible)
            {
                AddressesDataGrid.ItemsSource = db.Addresses
                    .OrderBy(a => a.Street)
                    .Select(a => new AddressViewModel
                    {
                        Id = a.AddressId,
                        Street = a.Street
                    })
                    .ToList();

                if (selectedAddress != null)
                    AddressesDataGrid.SelectedItem = ((List<AddressViewModel>)AddressesDataGrid.ItemsSource)
                        .FirstOrDefault(a => a.Id == selectedAddress.Id);
            }

            // ---------- Blocks ----------
            else if (BlocksDataGrid.Visibility == Visibility.Visible)
            {
                try
                {

                if (selectedAddress != null)
                {
                    BlocksDataGrid.ItemsSource = db.Blocks
                        .Where(b => b.AddressId == selectedAddress.Id)
                        .OrderBy(b => b.BlockName)
                        .Select(b => new BlockViewModel
                        {
                            Id = b.BlockId,
                            BlockName = b.BlockName,
                            Address = selectedAddress
                        })
                        .ToList();

                    if (selectedBlock != null)
                        BlocksDataGrid.SelectedItem = ((List<BlockViewModel>)BlocksDataGrid.ItemsSource)
                            .FirstOrDefault(b => b.Id == selectedBlock.Id);
                }
                else
                {
                    BlocksDataGrid.ItemsSource = new List<BlockViewModel>();
                }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Грешка: {ex.Message}\n\n{ex.StackTrace}",
                        "Грешка при зареждане на блокоше",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }

            // ---------- Entrances ----------
            else if (EntrancesDataGrid.Visibility == Visibility.Visible)
            {
                try
                {

                var blockForEntrances = selectedBlock ?? ((List<BlockViewModel>)BlocksDataGrid.ItemsSource).FirstOrDefault();

                if (blockForEntrances != null)
                {
                    var entrances = db.Entrances
                        .Where(e => e.BlockId == blockForEntrances.Id)
                        .OrderBy(e => e.EntranceName)
                        .Select(e => new EntranceViewModel
                        {
                            Id = e.EntranceId,
                            Name = e.EntranceName,
                            TotalAmount = db.TotalSums
                                .Where(ts => ts.EntranceId == e.EntranceId)
                                .Select(ts => (decimal?)ts.Summary)
                                .FirstOrDefault() ?? 0
                        })
                        .ToList();

                    EntrancesDataGrid.ItemsSource = entrances;

                    if (selectedEntrance != null && selectedEntrance.Id != 0)
                        EntrancesDataGrid.SelectedItem = entrances.FirstOrDefault(ev => ev.Id == selectedEntrance.Id);
                    else
                        EntrancesDataGrid.SelectedItem = null;
                }
                else
                {
                    EntrancesDataGrid.ItemsSource = new List<EntranceViewModel>();
                }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Грешка: {ex.Message}\n\n{ex.StackTrace}",
                        "Грешка при зареждане на входове",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }

            // ---------- Apartments ----------
            else if (ApartmentsDataGrid.Visibility == Visibility.Visible)
            {
                try
                {

                var selectedEntranceVM = EntrancesDataGrid.SelectedItem as EntranceViewModel;
                if (selectedEntranceVM != null)
                {
                    var apartmentsFromDb = db.Apartments
                        .Where(a => a.EntranceId == selectedEntranceVM.Id)
                        .Include(a => a.Owners)
                        .Include(a => a.Entrance)
                            .ThenInclude(e => e.Block)
                                .ThenInclude(b => b.Address)
                        .Include(a => a.ApartmentTransactions)
                            .ThenInclude(t => t.Category)
                        .ToList();

                    var apartments = apartmentsFromDb.Select(a =>
                    {
                        var expenseAmounts = a.ApartmentTransactions
                            .Where(t => t.Category.Kind != "Приход" && t.Category.Appliance == "apartments")
                            .GroupBy(t => t.Category.Name)
                            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                        var incomeAmounts = a.ApartmentTransactions
                            .Where(t => t.Category.Kind == "Приход" && t.Category.Appliance == "apartments")
                            .GroupBy(t => t.Category.Name)
                            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

                        var owner = a.Owners.FirstOrDefault();
                        var debt = db.Debts.FirstOrDefault(d => d.ApartmentId == a.ApartmentId);


                        return new ApartmentViewModel
                        {
                            ApartmentId = a.ApartmentId,
                            ApartmentNumber = a.ApartmentNumber,
                            OwnerName = owner?.OwnerName,
                            OwnerPhone = owner?.PhoneNumber,
                            Note = a.Note,
                            ResidentCount = a.ResidentCount ?? 0,
                            Street = a.Entrance?.Block?.Address?.Street,
                            BlockNumber = a.Entrance?.Block?.BlockName,
                            EntranceNumber = a.Entrance?.EntranceName,
                            ExpenseAmounts = expenseAmounts,
                            IncomeAmounts = incomeAmounts,
                              TotalSum = debt?.TotalSum ?? 0,  
                            PaidAmount = debt?.PaidSum ?? 0
                        };
                    })
                        .OrderBy(a=>a.ApartmentNumber)
                        .ToList();

                    ApartmentsDataGrid.ItemsSource = apartments;
                }
                else
                {
                    ApartmentsDataGrid.ItemsSource = new List<ApartmentViewModel>();
                }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Грешка: {ex.Message}\n\n{ex.StackTrace}",
                        "Грешка при зареждане на апартаменти",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }
        private void LoadApartments(int? addressId = null, int? blockId = null, int? entranceId = null)
        {
            using var db = new SyndiceoDBContext();

            var query = db.Apartments
                .Include(a => a.Entrance)
                    .ThenInclude(e => e.Block)
                        .ThenInclude(b => b.Address)
                .AsQueryable();

            if (addressId.HasValue)
                query = query.Where(a => a.Entrance.Block.AddressId == addressId.Value);
            if (blockId.HasValue)
                query = query.Where(a => a.Entrance.BlockId == blockId.Value);
            if (entranceId.HasValue)
                query = query.Where(a => a.EntranceId == entranceId.Value);

            var apartments = query.OrderBy(a => a.ApartmentNumber).ToList();

            var expenseCategories = db.Categories
                .Where(c => c.Kind != "Приход" && c.Appliance == "apartments")
                .OrderBy(c => c.Name)
                .ToList();

            var incomeCategories = db.Categories
                .Where(c => c.Kind == "Приход" && c.Appliance == "apartments")
                .OrderBy(c => c.Name)
                .ToList();
            var apartmentVMs = apartments.Select(a =>
            {
                var expenseDict = new Dictionary<string, decimal>();
                var incomeDict = new Dictionary<string, decimal>();

                foreach (var cat in expenseCategories)
                {
                    var amount = db.ApartmentTransactions
                        .Where(t => t.ApartmentId == a.ApartmentId && t.CategoryId == cat.Id)
                        .Sum(t => (decimal?)t.Amount) ?? 0m;

                    expenseDict[cat.Name] = amount;
                }

                foreach (var cat in incomeCategories)
                {
                    var amount = db.ApartmentTransactions
                        .Where(t => t.ApartmentId == a.ApartmentId && t.CategoryId == cat.Id)
                        .Sum(t => (decimal?)t.Amount) ?? 0m;

                    incomeDict[cat.Name] = amount;
                }

                var debt = db.Debts.FirstOrDefault(d => d.ApartmentId == a.ApartmentId);

                var total = debt?.TotalSum ?? expenseDict.Values.Sum();
                var paid = debt?.PaidSum ?? incomeDict.Values.Sum();

                return new ApartmentViewModel
                {
                    ApartmentId = a.ApartmentId,
                    ApartmentNumber = a.ApartmentNumber,
                    ExpenseAmounts = expenseDict,
                    IncomeAmounts = incomeDict,
                    ResidentCount = a.ResidentCount ?? 0,
                    TotalSum = total,
                    PaidAmount = paid,
                    IsMarked = paid >= total && total > 0
                };
            }).ToList();

            ApartmentsDataGrid.Columns.Clear();

            ApartmentsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Апартамент",
                Binding = new Binding("ApartmentNumber"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            foreach (var cat in expenseCategories)
            {
                ApartmentsDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = cat.Name,
                    Binding = new Binding($"ExpenseAmounts[{cat.Name}]") { StringFormat = "N2" },
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                    }
                });
            }

            foreach (var cat in incomeCategories)
            {
                ApartmentsDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = cat.Name,
                    Binding = new Binding($"IncomeAmounts[{cat.Name}]") { StringFormat = "N2" },
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                    }
                });
            }

            ApartmentsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Обща дължима сума",
                Binding = new Binding("TotalSum") { StringFormat = "N2" },
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                }
            });

            ApartmentsDataGrid.ItemsSource = apartmentVMs;
        }

        private int saveAddressId;
        private int saveBlockId;
        private int saveEntranceId;
        private void BreadcrumbAddresses_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            AddressesDataGrid.Visibility = Visibility.Visible;
            BlocksDataGrid.Visibility = Visibility.Hidden;
            EntrancesDataGrid.Visibility = Visibility.Hidden;
            ApartmentsDataGrid.Visibility = Visibility.Hidden;

            UpdateBreadcrumbs();
            ClearSelectionsBelow(AddressesDataGrid);

            using var db = new SyndiceoDBContext();
            AddressesDataGrid.ItemsSource = db.Addresses
                .OrderBy(a => a.Street)
                .Select(a => new AddressViewModel
                {
                    Id = a.AddressId,
                    Street = a.Street
                })
                .ToList();
        }

        private void BreadcrumbBlocks_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            AddressesDataGrid.Visibility = Visibility.Hidden;
            BlocksDataGrid.Visibility = Visibility.Visible;
            EntrancesDataGrid.Visibility = Visibility.Hidden;
            ApartmentsDataGrid.Visibility = Visibility.Hidden;

            UpdateBreadcrumbs();
            ClearSelectionsBelow(BlocksDataGrid);

            using var db = new SyndiceoDBContext();

            if (saveAddressId == 0)
            {
                var firstAddress = db.Addresses
                    .OrderBy(a => a.Street)
                    .Select(a => new AddressViewModel
                    {
                        Id = a.AddressId,
                        Street = a.Street
                    })
                    .FirstOrDefault();

                if (firstAddress != null)
                    saveAddressId = firstAddress.Id;
            }

            var blocks = db.Blocks
                .Where(b => b.AddressId == saveAddressId)
                .OrderBy(b => b.BlockName)
                .Select(b => new BlockViewModel
                {
                    Id = b.BlockId,
                    BlockName = b.BlockName,
                    Address = new AddressViewModel
                    {
                        Id = saveAddressId,
                        Street = b.Address.Street
                    }
                })
                .ToList();

            BlocksDataGrid.ItemsSource = blocks;

            if (blocks.Any())
            {
                if (saveBlockId == 0)
                    saveBlockId = blocks.First().Id;
            }
        }


        private void BreadcrumbEntrances_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            AddressesDataGrid.Visibility = Visibility.Hidden;
            BlocksDataGrid.Visibility = Visibility.Hidden;
            EntrancesDataGrid.Visibility = Visibility.Visible;
            ApartmentsDataGrid.Visibility = Visibility.Hidden;

            UpdateBreadcrumbs();
            ClearSelectionsBelow(EntrancesDataGrid);

            using var db = new SyndiceoDBContext();

            if (saveBlockId == 0)
            {
                var firstBlock = db.Blocks
                    .Where(b => b.AddressId == saveAddressId)
                    .OrderBy(b => b.BlockName)
                    .Select(b => new BlockViewModel
                    {
                        Id = b.BlockId,
                        BlockName = b.BlockName
                    })
                    .FirstOrDefault();

                if (firstBlock != null)
                    saveBlockId = firstBlock.Id;
            }

            var entrances = db.Entrances
                .Where(en => en.BlockId == saveBlockId)
                .OrderBy(en => en.EntranceName)
                .Select(en => new EntranceViewModel
                {
                    Id = en.EntranceId,
                    Name = en.EntranceName,
                    TotalAmount =
                        (db.ApartmentTransactions
                            .Where(t => t.Apartment.EntranceId == en.EntranceId && t.Category.Kind != "Приход")
                            .Sum(t => (decimal?)t.Amount) ?? 0)
                      - (db.ApartmentTransactions
                            .Where(t => t.Apartment.EntranceId == en.EntranceId && t.Category.Kind == "Приход")
                            .Sum(t => (decimal?)t.Amount) ?? 0)
                })
                .ToList();

            EntrancesDataGrid.ItemsSource = entrances;

            if (entrances.Any())
            {
                if (saveEntranceId == 0)
                    saveEntranceId = entrances.First().Id;
            }
        }


        private void BreadcrumbApartments_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            AddressesDataGrid.Visibility = Visibility.Hidden;
            BlocksDataGrid.Visibility = Visibility.Hidden;
            EntrancesDataGrid.Visibility = Visibility.Hidden;
            ApartmentsDataGrid.Visibility = Visibility.Visible;

            UpdateBreadcrumbs();
            LoadApartments(saveAddressId,saveBlockId,saveEntranceId);
        }
        private void ClearSelectionsBelow(DataGrid currentDataGrid)
        {
            if (currentDataGrid != AddressesDataGrid)
                AddressesDataGrid.SelectedItem = null;

            if (currentDataGrid != BlocksDataGrid)
                BlocksDataGrid.SelectedItem = null;

            if (currentDataGrid != EntrancesDataGrid)
                EntrancesDataGrid.SelectedItem = null;

            if (currentDataGrid != ApartmentsDataGrid)
                ApartmentsDataGrid.SelectedItem = null;
        }

        private void AddressesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedAddress = AddressesDataGrid.SelectedItem as AddressViewModel;

            if (selectedAddress == null && e.OriginalSource is FrameworkElement fe)
            {
                selectedAddress = fe.DataContext as AddressViewModel;
            }

            if (selectedAddress == null) return;

            AddressesDataGrid.Visibility = Visibility.Hidden;
            ShowWatermark();
            HideWatermark(300);
            BlocksDataGrid.Visibility = Visibility.Visible;

            using var context = new SyndiceoDBContext();
            BlocksDataGrid.ItemsSource = context.Blocks
                .Where(b => b.AddressId == selectedAddress.Id)
                .OrderBy(b => b.BlockName)
                .Select(b => new BlockViewModel
                {
                    Id = b.BlockId,
                    BlockName = b.BlockName,
                    Address = new AddressViewModel
                    {
                        Id = selectedAddress.Id,
                        Street = selectedAddress.Street
                    }
                })
                .ToList();

            UpdateBreadcrumbText(selectedAddress);
            UpdateBreadcrumbs();
        }

        private void BlocksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BlocksDataGrid.SelectedItem is not BlockViewModel selectedBlock) return;

            BlocksDataGrid.Visibility = Visibility.Hidden;
            ShowWatermark();
            HideWatermark(300);
            EntrancesDataGrid.Visibility = Visibility.Visible;

            using var context = new SyndiceoDBContext();

            var entrances = context.Entrances
                                   .Where(en => en.BlockId == selectedBlock.Id)
                                   .OrderBy(en => en.EntranceName)
                                   .Select(en => new EntranceViewModel
                                   {
                                       Id = en.EntranceId,
                                       Name = en.EntranceName,
                                       TotalAmount =
                                           (context.ApartmentTransactions
                                                   .Where(t => t.Apartment.EntranceId == en.EntranceId && t.Category.Kind != "Приход")
                                                   .Sum(t => (decimal?)t.Amount) ?? 0)
                                         - (context.ApartmentTransactions
                                                   .Where(t => t.Apartment.EntranceId == en.EntranceId && t.Category.Kind == "Приход")
                                                   .Sum(t => (decimal?)t.Amount) ?? 0),
                                       Block = new BlockViewModel
                                       {
                                           Id = selectedBlock.Id,
                                           BlockName = selectedBlock.BlockName,
                                           Address = selectedBlock.Address
                                       }
                                   })
                                   .ToList();

            EntrancesDataGrid.Columns.Clear();

            EntrancesDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Вход",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            EntrancesDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Обща дължима сума за целия вход",
                Binding = new Binding("TotalAmount") { StringFormat = "N2" },
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                },
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            EntrancesDataGrid.ItemsSource = entrances;

            UpdateBreadcrumbText(selectedBlock);
            UpdateBreadcrumbs();
        }

        private void EntrancesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int entranceId = 0;
            if (EntrancesDataGrid.SelectedItem is EntranceViewModel evm)
                entranceId = evm.Id;
            else if (EntrancesDataGrid.SelectedItem is Entrance en)
                entranceId = en.EntranceId;
            else
                return;

            EntrancesDataGrid.Visibility = Visibility.Hidden;
            ShowWatermark();
            HideWatermark(300);
            ApartmentsDataGrid.Visibility = Visibility.Visible;

            using var context = new SyndiceoDBContext();
            var selectedEntrance = context.Entrances
                                          .Include(e => e.Block)
                                          .ThenInclude(b => b.Address)
                                          .FirstOrDefault(e => e.EntranceId == entranceId);

            if (selectedEntrance == null) return;

            var selectedBlock = selectedEntrance.Block;
            var selectedAddress = selectedBlock?.Address;

            LoadApartments(
                addressId: selectedAddress?.AddressId,
                blockId: selectedBlock?.BlockId,
                entranceId: selectedEntrance.EntranceId
            );

            UpdateBreadcrumbText(EntrancesDataGrid.SelectedItem);
            UpdateBreadcrumbs();
        }

        private void ApartmentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ApartmentsDataGrid.SelectedItem is ApartmentViewModel selectedApartment)
            {
                UpdateBreadcrumbText(selectedApartment);

                var showWindow = new ShowApartmentWindow(selectedApartment);
                showWindow.ShowDialog();
            }

        }

        private void NotesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NotesTextBox.Text == "*Тук може да се водят напомняния и лични записки*")
            {
                NotesTextBox.Text = "";
                NotesTextBox.Foreground = new SolidColorBrush(Colors.Black);
                NotesTextBox.FontStyle = FontStyles.Normal;
            }
        }

        private void NotesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NotesTextBox.Text))
            {
                NotesTextBox.Text = "*Тук може да се водят напомняния и лични записки*";
                NotesTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                NotesTextBox.FontStyle = FontStyles.Italic;
            }
            SaveNotes();
        }

        private void searchTxtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchTxtBox.Text == "Име,Тел. номер")
            {
                searchTxtBox.Text = "";
                searchTxtBox.Foreground = new SolidColorBrush(Colors.Black);
                searchTxtBox.FontStyle = FontStyles.Normal;
            }
        }

        private void searchTxtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTxtBox.Text))
            {
                searchTxtBox.Text = "Име,Тел. номер";
                searchTxtBox.Foreground = new SolidColorBrush(Colors.Gray);
                searchTxtBox.FontStyle = FontStyles.Italic;
            }
        }

        private void UpdateEntranceMarking(EntranceViewModel entrance)
        {
            if (entrance == null) return;

            entrance.IsFullyMarked = Apartments
                .Where(a => a.Entrance != null && a.Entrance.Id == entrance.Id)
                .All(a => a.IsMarked);
        }

        private void MarkApartment(ApartmentViewModel apartment)
        {
            if (apartment == null) return;

            using var context = new SyndiceoDBContext();

            var ap = context.Apartments
                            .Include(a => a.Entrance)
                            .FirstOrDefault(a => a.ApartmentId == apartment.ApartmentId);

            if (ap == null) return;

            ap.IsMarked = true;
            context.SaveChanges();

            apartment.IsMarked = true;
            UpdateEntranceMarking(apartment.Entrance);
            ApplyPaymentMarkingToApartments();

            var summaryWindow = new SummaryPriceWindow(apartment.ApartmentId);
            summaryWindow.Owner = this;
            summaryWindow.ShowDialog();
            LoadData();
            ApplyPaymentMarkingToApartments();


        }

        private void UnmarkApartment(ApartmentViewModel apartment)
        {
            if (apartment == null) return;

            using var context = new SyndiceoDBContext();

            var ap = context.Apartments
                            .Include(a => a.Entrance)
                            .FirstOrDefault(a => a.ApartmentId == apartment.ApartmentId);

            if (ap == null) return;

            var debt = context.Debts.FirstOrDefault(d => d.ApartmentId == apartment.ApartmentId);
            if (debt != null && debt.PaidSum > 0)
            {
                var entranceId = ap.EntranceId;
                var amountToRemove = debt.PaidSum;

                debt.PaidSum = 0;
                context.SaveChanges();

                var category = context.Categories
                                      .SingleOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход");

                if (category != null)
                {
                    var entranceTransaction = context.EntranceTransactions
                                                     .FirstOrDefault(t => t.EntranceId == entranceId &&
                                                                          t.CategoryId == category.Id);

                    if (entranceTransaction != null)
                    {
                        entranceTransaction.Amount -= amountToRemove;

                        if (entranceTransaction.Amount <= 0)
                        {
                            entranceTransaction.Amount = 0;
                        }

                        context.SaveChanges();
                    }
                }
            }

            ap.IsMarked = false;
            apartment.PaidAmount = 0;
            apartment.IsMarked = false;

            context.SaveChanges();
            ApplyPaymentMarkingToApartments();
        }

        private void MarkEntrance(EntranceViewModel entrance)
        {
            if (entrance == null) return;

            using var context = new SyndiceoDBContext();
            var apartmentsInDb = context.Apartments.Where(a => a.EntranceId == entrance.Id).ToList();
            foreach (var ap in apartmentsInDb)
                ap.IsMarked = true;
            context.SaveChanges();

            foreach (var vm in Apartments.Where(a => a.Entrance.Id == entrance.Id))
                vm.IsMarked = true;

            UpdateEntranceMarking(entrance);
            ApplyPaymentMarkingToApartments();
        }

        private void UnmarkEntrance(EntranceViewModel entrance)
        {
            if (entrance == null) return;

            using var context = new SyndiceoDBContext();
            var apartmentsInDb = context.Apartments.Where(a => a.EntranceId == entrance.Id).ToList();
            foreach (var ap in apartmentsInDb)
                ap.IsMarked = false;
            context.SaveChanges();

            foreach (var vm in Apartments.Where(a => a.Entrance.Id == entrance.Id))
                vm.IsMarked = false;

            UpdateEntranceMarking(entrance);
            ApplyPaymentMarkingToApartments();
        }
        private void ApplyPaymentMarkingToApartments()
        {
            foreach (var item in ApartmentsDataGrid.Items)
            {
                if (item is ApartmentViewModel apartment)
                {
                    var row = (DataGridRow)ApartmentsDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                    if (row == null)
                    {
                        ApartmentsDataGrid.UpdateLayout();
                        ApartmentsDataGrid.ScrollIntoView(item);
                        row = (DataGridRow)ApartmentsDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                    }


                    if (row != null)
                    {
                        if (apartment.PaidAmount >= apartment.TotalSum && apartment.TotalSum > 0)
                            row.Background = Brushes.LightGreen;
                        else if (apartment.IsPartiallyPaid)
                            row.Background = Brushes.LightGoldenrodYellow;
                        else
                            row.Background = Brushes.White;
                    }
                }
            }
        }
        private void MarkInGreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApartmentsDataGrid.SelectedItem is ApartmentViewModel selectedApartment)
            {
                MarkApartment(selectedApartment);
            }
            else if (EntrancesDataGrid.SelectedItem is EntranceViewModel selectedEntrance)
            {
                MarkEntrance(selectedEntrance); 
            }
        }

        private void UnmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApartmentsDataGrid.SelectedItem is ApartmentViewModel selectedApartment)
                UnmarkApartment(selectedApartment);
            else if (EntrancesDataGrid.SelectedItem is EntranceViewModel selectedEntrance)
                UnmarkEntrance(selectedEntrance);
        }
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            var selectedApartmentVM = ApartmentsDataGrid.SelectedItem as ApartmentViewModel;
            if (selectedApartmentVM == null)
            {
                MessageBox.Show("Моля, изберете апартамент.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new SyndiceoDBContext();
            var apartment = db.Apartments.Find(selectedApartmentVM.ApartmentId);

            if (noteWindow == null || !noteWindow.IsLoaded) 
            {
                noteWindow = new AddNoteWindow(apartment);
                noteWindow.ShowDialog();

                NotesPreview.Text = string.IsNullOrWhiteSpace(apartment?.Note)
                    ? "*Няма записани бележки за този апартамент*"
                    : apartment.Note;

                ApartmentsDataGrid.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Прозорецът за добавяне на бележки вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                noteWindow.Activate();
            }

        }

        private AddNoteWindow noteWindow;
        private DocumentsWindow docWd;
        private TaxesWindow taxesWd;
        private UpdateWindow updateWd;
        private PrintWIndow printWd;
        private SearchWindow searchWindow;
        private AboutWindow aboutWindow;
        private MaintenanceHistoryWindow maintenanceHistoryWd;
        private SettingsWindow settingsWd;
        private ArchiveWindow archiveWd;

        private void ApartmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedApartmentVM = ApartmentsDataGrid.SelectedItem as ApartmentViewModel;

            if (selectedApartmentVM != null)
            {
                using var db = new SyndiceoDBContext();
                var apartment = db.Apartments.Find(selectedApartmentVM.ApartmentId);

                NotesPreview.Text = string.IsNullOrWhiteSpace(apartment?.Note)
                    ? "*Няма записани бележки за този апартамент*"
                    : apartment.Note;
                if (!string.IsNullOrWhiteSpace(apartment?.Note))
                {
                    AddNoteButton.Content = "📝 Промени бележка";
                }
                else
                {
                    AddNoteButton.Content = "📝 Добави бележка";

                }
            }
            else
            {
                NotesPreview.Text = "*Тук ще се показват бележките за избраният от вас апартамент*";
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (printWd == null || !printWd.IsLoaded)
            {
                using var db = new SyndiceoDBContext();

                var selectedItem = EntrancesDataGrid.SelectedItem as EntranceViewModel;
                if (selectedItem == null)
                {
                    MessageBox.Show("Моля, изберете вход.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int entranceId = selectedItem.Id;

                var incomes = db.EntranceTransactions
                    .Include(t => t.Category)
                    .Where(t => t.EntranceId == entranceId && t.Category.Kind == "Приход")
                    .Select(t => new TransactionViewModel
                    {
                        Amount = t.Amount,
                        CategoryName = t.Category != null ? t.Category.Name : t.Description,
                        Description = t.Description
                    }).ToList();

                var expenses = db.EntranceTransactions
                    .Include(t => t.Category)
                    .Where(t => t.EntranceId == entranceId && t.Category.Kind == "Разход")
                    .Select(t => new TransactionViewModel
                    {
                        Amount = t.Amount,
                        CategoryName = t.Category != null ? t.Category.Name : t.Description,
                        Description = t.Description
                    }).ToList();

                var cashboxEntry = db.Cashboxes.FirstOrDefault(c => c.EntranceId == entranceId);
                decimal cashbox = cashboxEntry?.CurrentBalance ?? 0m;

                var apartments = db.Apartments
                                   .Include(a => a.Entrance.Block.Address)
                                   .Where(a => a.EntranceId == entranceId)
                                   .ToList();

                string fullAddress = string.Join("; ", apartments.Select(a =>
                    $"{a.Entrance.Block.Address.Street}, Блок {a.Entrance.Block.BlockName}, Вход {a.Entrance.EntranceName}"
                ));

                printWd = new PrintWIndow(incomes, expenses, cashbox, entranceId, fullAddress);
                printWd.Closed += (s, args) => {
                    if (Properties.Settings.Default.isReportDone == true)
                    {
                        using var db2 = new SyndiceoDBContext();

                        foreach (var apVm in Apartments)
                        {
                            apVm.IsMarked = false;

                            var apDb = db2.Apartments.Find(apVm.ApartmentId);
                            if (apDb != null)
                                apDb.IsMarked = false;
                        }

                        db2.SaveChanges();
                    }

                };
                printWd.Closed += (s, args) => LoadData();
                printWd.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за отчети вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                printWd.Activate();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (updateWd == null || !updateWd.IsLoaded)
            {
                updateWd = new UpdateWindow();
                updateWd.Closed += (s, args) => LoadData();
                updateWd.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за актуализации вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                updateWd.Activate();
            }
        }

        private void searchInDB_Click(object sender, RoutedEventArgs e)
        {
            string search = searchTxtBox.Text;
            Properties.Settings.Default.currentSearch = search;

            if (searchWindow == null || !searchWindow.IsLoaded)
            {
                searchWindow = new SearchWindow();
                searchWindow.Closed += (s, e) => LoadData();
                searchWindow.Closed += (s, e) => searchTxtBox.Clear();
                searchWindow.Closed += (s, e) => Keyboard.ClearFocus();
                searchWindow.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за търсене вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                searchWindow.Activate();
            }
        }
        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            if (aboutWindow == null || !aboutWindow.IsLoaded)
            {
                aboutWindow = new AboutWindow();
                aboutWindow.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за относно приложението вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                aboutWindow.Activate();
            }
        }
        private void MaintenanceHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            int? selectedEntranceId = null;

            if (EntrancesDataGrid.SelectedItem is EntranceViewModel evm)
                selectedEntranceId = evm.Id;
            else if (EntrancesDataGrid.SelectedItem is Entrance en)
                selectedEntranceId = en.EntranceId;

            if (!selectedEntranceId.HasValue)
            {
                MessageBox.Show("Моля, изберете вход от списъка.", "Грешка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var context = new SyndiceoDBContext();
            var selectedEntrance = context.Entrances
                                          .Include(e => e.Block)
                                          .ThenInclude(b => b.Address)
                                          .FirstOrDefault(e => e.EntranceId == selectedEntranceId.Value);

            if (selectedEntrance == null)
            {
                MessageBox.Show("Входът не беше намерен в базата данни.", "Грешка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (maintenanceHistoryWd == null || !maintenanceHistoryWd.IsLoaded)
            {
                maintenanceHistoryWd = new MaintenanceHistoryWindow(selectedEntrance);
                maintenanceHistoryWd.Show();
                maintenanceHistoryWd.Closed += (s, args) => LoadData();
            }
            else
            {
                MessageBox.Show("Прозорецът за ремонти вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                maintenanceHistoryWd.Activate();
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWd == null || !settingsWd.IsLoaded)
            {
                settingsWd = new SettingsWindow();
                settingsWd.Closed += (s, args) => LoadData();
                settingsWd.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за настройки на приложението вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                settingsWd.Activate();
            }
        }
        private void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (archiveWd == null || !archiveWd.IsLoaded)
            {
                archiveWd = new ArchiveWindow();
                archiveWd.Closed += (s, args) => LoadData();
                archiveWd.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за архиви вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                archiveWd.Activate();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var openWindows = Application.Current.Windows
     .OfType<Window>()
     .Where(w => w != this && w.IsLoaded && w.Visibility == Visibility.Visible && w.GetType().Name != "AdornerWindow")
     .ToList();

            foreach (var w in Application.Current.Windows.OfType<Window>())
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DEBUG] Window: {w.Title} | Loaded={w.IsLoaded} | Visible={w.Visibility} | Type={w.GetType().Name}"
                );
            }
            if (openWindows.Any())
            {
                MessageBox.Show("Моля, затворете всички други отворени прозорци, преди да излезете.",
                                "Отворени прозорци",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            bool autoArchiveEnabled = Properties.Settings.Default.AutoArchive;

            if (!autoArchiveEnabled)
                return;

            try
            {
                string connectionString = "Server=(LocalDB)\\MSSQLLocalDB;Database=SyndiceoDB;Trusted_Connection=True;";

                string roamingFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Syndiceo", "Backups"

                );

                if (!Directory.Exists(roamingFolder))
                    Directory.CreateDirectory(roamingFolder);

                BackupDatabase(connectionString, roamingFolder);
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Syndiceo",
                        "BackupErrors.log"
                    ),
                    DateTime.Now + " - Грешка при архивиране: " + ex.Message + Environment.NewLine
                );
            }
            Properties.Settings.Default.MainWindowClosing = true;
            ArchiveWindow a = new ArchiveWindow();
            a.Show();
            Properties.Settings.Default.WindowState = this.WindowState.ToString();
            Properties.Settings.Default.Save();
        }

        private void BackupDatabase(string connectionString, string backupFolder)
        {
            string dbName = "SyndiceoDB";
            string backupFile = System.IO.Path.Combine(backupFolder, $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = $@"BACKUP DATABASE [{dbName}] 
                        TO DISK = N'{backupFile}' 
                        WITH FORMAT, INIT, NAME = N'Backup на базата {dbName}'";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            File.AppendAllText(
                System.IO.Path.Combine(backupFolder, "BackupLog.txt"),
                DateTime.Now + " - Архивирано успешно: " + backupFile + Environment.NewLine
            );
            Properties.Settings.Default.LastBackupPath = backupFolder;
            Properties.Settings.Default.Save();
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntrance = EntrancesDataGrid.SelectedItem as EntranceViewModel;

            if (docWd == null || !docWd.IsLoaded)
            {
                docWd = new DocumentsWindow();

                if (selectedEntrance != null)
                {
                    docWd.LoadEntranceDocuments(selectedEntrance.Id);
                    docWd.SetAddPanelEnabled(true);
                }
                else
                {
                    docWd.LoadEntranceDocuments();
                    docWd.SetAddPanelEnabled(false);
                }

                docWd.Closed += (s, args) => LoadData();
                docWd.Show();
            }
            else
            {
                MessageBox.Show("Прозорецът за документи вече е отворен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                docWd.Activate();
            }
        }

        private void ManageTaxesButton_Click(object sender, RoutedEventArgs e)
        {
            using var db = new SyndiceoDBContext();

            if (ApartmentsDataGrid.SelectedItem is ApartmentViewModel selectedApartmentVM)
            {
                if (taxesWd == null || !taxesWd.IsLoaded)
                {
                    taxesWd = new TaxesWindow(apartmentId: selectedApartmentVM.ApartmentId);
                    taxesWd.Closed += (s, args) => LoadData();
                    taxesWd.Show();
                }
                else
                {
                    taxesWd.Activate();
                }

                var apartment = db.Apartments
                                  .Include(a => a.Entrance)
                                  .ThenInclude(e => e.Block)
                                  .ThenInclude(b => b.Address)
                                  .FirstOrDefault(a => a.ApartmentId == selectedApartmentVM.ApartmentId);

                if (apartment != null)
                {
                    LoadApartments(addressId: apartment.Entrance.Block.AddressId,
                                   blockId: apartment.Entrance.BlockId,
                                   entranceId: apartment.EntranceId);
                    ApartmentsDataGrid.Visibility = Visibility.Visible;
                    EntrancesDataGrid.Visibility = Visibility.Hidden;
                }

                UpdateBreadcrumbText(selectedApartmentVM);
                UpdateBreadcrumbs();
                return;
            }

            int? selectedEntranceId = null;
            if (EntrancesDataGrid.SelectedItem is Entrance en)
                selectedEntranceId = en.EntranceId;
            else if (EntrancesDataGrid.SelectedItem is EntranceViewModel evm)
                selectedEntranceId = evm.Id;

            if (selectedEntranceId.HasValue)
            {
                if (taxesWd == null || !taxesWd.IsLoaded)
                {
                    taxesWd = new TaxesWindow(entranceId: selectedEntranceId.Value);
                    taxesWd.Closed += (s, args) => LoadData();
                    taxesWd.Show();
                    if(ApartmentsDataGrid.Visibility==Visibility.Visible)
                    {
                        MessageBox.Show("В момента редактирате таксите за вход!", "ИНФОРМАЦИЯ");
                    }
                }
                else
                {
                    taxesWd.Close(); // затваряме стария
                    taxesWd = new TaxesWindow(entranceId: selectedEntranceId.Value); // нов за правилния вход
                    taxesWd.Closed += (s, args) => LoadData();
                    taxesWd.Show();
                }


                LoadData();
                UpdateBreadcrumbText(selectedEntranceId);
                UpdateBreadcrumbs();
                return;
            }

            MessageBox.Show("Моля, изберете апартамент или вход.",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadEntrances()
        {
            using var context = new SyndiceoDBContext();

            var entrances = context.Entrances.ToList();

            foreach (var entrance in entrances)
            {
                var expenses = context.ApartmentTransactions
                                      .Where(t => t.Apartment.EntranceId == entrance.EntranceId && t.Category.Kind != "Приход")
                                      .Sum(t => (decimal?)t.Amount) ?? 0;

                var collectedFees = context.ApartmentTransactions
                                           .Where(t => t.Apartment.EntranceId == entrance.EntranceId && t.Category.Kind == "Приход")
                                           .Sum(t => (decimal?)t.Amount) ?? 0;

                Entrances.Add(new EntranceViewModel
                {
                    Id = entrance.EntranceId,
                    Name = entrance.EntranceName,
                    TotalAmount = expenses - collectedFees
                });
            }
        }
        private void searchTxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        //Collections
        public ObservableCollection<AddressViewModel> Addresses { get; set; } = new ObservableCollection<AddressViewModel>();
        public ObservableCollection<BlockViewModel> Blocks { get; set; } = new ObservableCollection<BlockViewModel>();
        public ObservableCollection<EntranceViewModel> Entrances { get; set; } = new ObservableCollection<EntranceViewModel>();
        public ObservableCollection<ApartmentViewModel> Apartments { get; set; } = new ObservableCollection<ApartmentViewModel>();

        //Viewmodels

        public class ApartmentViewModel : INotifyPropertyChanged
        {
            private bool _isMarked = false;
            private decimal _paidAmount;
            private decimal _totalSum;

            public int ApartmentId { get; set; }
            public int ApartmentNumber { get; set; }
            public string Note { get; set; }
            public string Street { get; set; }
            public string BlockNumber { get; set; }
            public string EntranceNumber { get; set; }
            public string OwnerName { get; set; }
            public string OwnerPhone { get; set; }
            public Dictionary<string, decimal> ExpenseAmounts { get; set; } = new();
            public Dictionary<string, decimal> IncomeAmounts { get; set; } = new();
            public int ResidentCount { get; set; }
            public EntranceViewModel Entrance { get; set; }

            public decimal TotalSum
            {
                get => _totalSum;
                set
                {
                    if (_totalSum != value)
                    {
                        _totalSum = value;
                        OnPropertyChanged(nameof(TotalSum));
                        OnPropertyChanged(nameof(RemainingSum));
                        OnPropertyChanged(nameof(PaymentStatusColor));
                    }
                }
            }

            public decimal PaidAmount
            {
                get => _paidAmount;
                set
                {
                    if (_paidAmount != value)
                    {
                        _paidAmount = value;
                        OnPropertyChanged(nameof(PaidAmount));
                        OnPropertyChanged(nameof(RemainingSum));
                        OnPropertyChanged(nameof(IsPartiallyPaid));
                        OnPropertyChanged(nameof(PaymentStatusColor));
                        IsMarked = _paidAmount >= TotalSum && TotalSum > 0;
                    }
                }
            }

            public decimal RemainingSum => TotalSum - PaidAmount;
            public bool IsPartiallyPaid => PaidAmount > 0 && PaidAmount < TotalSum;

            public bool IsMarked
            {
                get => _isMarked;
                set
                {
                    if (_isMarked != value)
                    {
                        _isMarked = value;
                        OnPropertyChanged(nameof(IsMarked));
                    }
                }
            }

            public System.Windows.Media.Brush PaymentStatusColor
            {
                get
                {
                    if (PaidAmount >= TotalSum && TotalSum > 0)
                        return Brushes.LightGreen;           // напълно платено
                    else if (IsPartiallyPaid)
                        return Brushes.LightGoldenrodYellow; // частично платено
                    else
                        return Brushes.White;                // не е платено
                }
            }

            public void UpdatePayment(decimal newlyPaid)
            {
                PaidAmount += newlyPaid; // добавяме новоплатената сума
                if (PaidAmount > TotalSum)
                    PaidAmount = TotalSum;

                IsMarked = RemainingSum == 0; // маркираме само ако няма останало
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public class EntranceViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal TotalAmount { get; set; }

            public BlockViewModel Block { get; set; }

            public string Street => Block?.Address?.Street ?? "";
            public string BlockNumber => Block?.BlockName ?? "";

            public string OwnerName { get; set; }
            public string OwnerPhone { get; set; }
            private bool _isFullyMarked;
            public bool IsFullyMarked
            {
                get => _isFullyMarked;
                set
                {
                    if (_isFullyMarked != value)
                    {
                        _isFullyMarked = value;
                    }
                }
            }

        }

        public class BlockViewModel
        {
            public int Id { get; set; }
            public string BlockName { get; set; }

            public AddressViewModel Address { get; set; }
        }

        public class AddressViewModel
        {
            public int Id { get; set; }
            public string Street { get; set; }
        }

        private void AddressesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AddressesDataGrid.SelectedItem is AddressViewModel selectedAddress)
            {
                saveAddressId = selectedAddress.Id;
            }
        }
        private void BlockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlocksDataGrid.SelectedItem is BlockViewModel selectedBlcok)
            {
                saveBlockId = selectedBlcok.Id;
            }
        }
        private void EntranceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntrancesDataGrid.SelectedItem is EntranceViewModel en)
            {
                saveEntranceId = en.Id;
            }
        }

        private void openSiteButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = @"D:\Syndiceo\SyndiceoWeb\bin\Debug\net8.0\SyndiceoWeb.exe",
                UseShellExecute = true
            });
        }
    }
}
