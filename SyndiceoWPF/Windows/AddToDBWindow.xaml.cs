using Syndiceo.Data.Models;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Syndiceo.Windows
{
    public partial class AddToDBWindow : Window
    {
        public AddToDBWindow(string street = "", string blockName = "", string entranceName = "")
        {
            InitializeComponent();

            AddressComboBox.Text = street;
            BlockComboBox.Text = blockName;
            EntranceComboBox.Text = entranceName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string addressText = AddressComboBox.Text.Trim();
            string blockText = BlockComboBox.Text.Trim();
            string entranceText = CleanEntranceText(EntranceComboBox.Text);
            string ownerName = ownerNameTxtBox.Text.Trim();
            string ownerPhone = ownerPhoneNumberTxtBox.Text.Trim();
            string noteText = NoteTextBox.Text.Trim();
            string aptInput = ApartmentNumberTextBox.Text.Trim();
            string residentCountInput = ResidentsCountTextBox.Text.Trim();

            using (var context = new SyndiceoDBContext())
            {
                if (string.IsNullOrWhiteSpace(addressText))
                {
                    MessageBox.Show("Моля въведете поне адрес!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var address = context.Addresses.FirstOrDefault(a => a.Street == addressText);
                if (address == null)
                {
                    address = new Address { Street = addressText };
                    context.Addresses.Add(address);
                    context.SaveChanges();
                }

                Block block = null;
                if (!string.IsNullOrWhiteSpace(blockText))
                {
                    block = context.Blocks.FirstOrDefault(b => b.BlockName == blockText && b.AddressId == address.AddressId);
                    if (block == null)
                    {
                        block = new Block { BlockName = blockText, AddressId = address.AddressId };
                        context.Blocks.Add(block);
                        context.SaveChanges();
                    }
                }

                Entrance entrance = null;
                if (!string.IsNullOrWhiteSpace(entranceText) && block != null)
                {
                    entrance = context.Entrances.FirstOrDefault(e => e.EntranceName == entranceText && e.BlockId == block.BlockId);
                    if (entrance == null)
                    {
                        entrance = new Entrance { EntranceName = entranceText, BlockId = block.BlockId };
                        context.Entrances.Add(entrance);
                        context.SaveChanges();
                    }
                }

                Apartment apartment = null;
                if (!string.IsNullOrWhiteSpace(aptInput))
                {
                    if (!int.TryParse(aptInput, out int apartmentNumber))
                    {
                        MessageBox.Show("Номерът на апартамента трябва да бъде число!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (apartmentNumber <= 0)
                    {
                        MessageBox.Show("Номерът на апартамента трябва да бъде положително число!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (entrance != null)
                    {
                        apartment = context.Apartments
                            .FirstOrDefault(a => a.ApartmentNumber == apartmentNumber && a.EntranceId == entrance.EntranceId);

                        if (apartment != null)
                        {
                            MessageBox.Show($"Апартамент {apartmentNumber} вече съществува в този вход!",
                                "Дублиран запис", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        int residentCount = 0;
                        if (!string.IsNullOrWhiteSpace(residentCountInput))
                        {
                            if (!int.TryParse(residentCountInput, out residentCount))
                            {
                                MessageBox.Show("Броят на живущите трябва да бъде число.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (residentCount < 0)
                            {
                                MessageBox.Show("Броят на живущите не може да бъде отрицателен!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        apartment = new Apartment
                        {
                            ApartmentNumber = apartmentNumber,
                            Note = noteText,
                            IsMarked = false,
                            EntranceId = entrance.EntranceId,
                            ResidentCount = residentCount
                        };

                        context.Apartments.Add(apartment);
                        context.SaveChanges();
                    }
                    else
                    {
                        MessageBox.Show("За добавяне на апартамент, трябва да е избран вход.",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(ownerName))
                {
                    if (apartment == null)
                    {
                        MessageBox.Show("За добавяне на собственик, трябва да е добавен апартамент.",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(ownerPhone))
                    {
                        MessageBox.Show("Моля въведете телефонен номер на собственика!",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var ownerExists = context.Owners.FirstOrDefault(o =>
                        o.ApartmentId == apartment.ApartmentId &&
                        (o.OwnerName.ToLower() == ownerName.ToLower() ||
                         o.PhoneNumber == ownerPhone));

                    if (ownerExists != null)
                    {
                        MessageBox.Show($"Собственик с име или телефон вече съществува за апартамент {apartment.ApartmentNumber}!",
                            "Дублиран запис", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var owner = new Owner
                    {
                        OwnerName = ownerName,
                        PhoneNumber = ownerPhone,
                        ApartmentId = apartment.ApartmentId
                    };
                    context.Owners.Add(owner);
                    context.SaveChanges();
                }

                MessageBox.Show("Записът е успешно добавен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            this.Close();
        }

        private void AddressComboBox_DropDownOpened(object sender, EventArgs e)
        {
            using var context = new SyndiceoDBContext();
            AddressComboBox.ItemsSource = context.Addresses.OrderBy(a => a.Street).ToList();
        }

        private void BlockComboBox_DropDownOpened(object sender, EventArgs e)
        {
            using var context = new SyndiceoDBContext();
            if (AddressComboBox.SelectedItem is Address selectedAddress)
            {
                BlockComboBox.ItemsSource = context.Blocks
                    .Where(b => b.AddressId == selectedAddress.AddressId)
                    .OrderBy(b => b.BlockName)
                    .ToList();
            }
            else
            {
                BlockComboBox.ItemsSource = null;
            }
        }

        private void EntranceComboBox_DropDownOpened(object sender, EventArgs e)
        {
            using var context = new SyndiceoDBContext();
            if (BlockComboBox.SelectedItem is Block selectedBlock)
            {
                EntranceComboBox.ItemsSource = context.Entrances
                    .Where(ent => ent.BlockId == selectedBlock.BlockId)
                    .OrderBy(ent => ent.EntranceName)
                    .ToList();
            }
            else
            {
                EntranceComboBox.ItemsSource = null;
            }
        }

        private string CleanEntranceText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            string[] patterns = { "вход", "вх.", "вх" };
            string result = input;

            foreach (var pattern in patterns)
            {
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    pattern,
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }

            return result.Trim();
        }

        private void ownerPhoneNumberTxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int length = ownerPhoneNumberTxtBox.Text.Length;
            if (length > 10)
                ownerPhoneNumberTxtBox.ToolTip = "ВНИМАНИЕ: ТЕЛЕФОННИЯ НОМЕР Е ПО-ДЪЛЪГ ОТ 10 ЦИФРИ";
            else if (length < 8)
                ownerPhoneNumberTxtBox.ToolTip = "ВНИМАНИЕ: ТЕЛЕФОННИЯ НОМЕР Е ПО-КРАТЪК ОТ 8 ЦИФРИ";
            else
                ownerPhoneNumberTxtBox.ClearValue(ToolTipProperty);
        }

        private void AddressComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string addressText = AddressComboBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(addressText))
            {
                using var context = new SyndiceoDBContext();
                var existingAddress = context.Addresses.FirstOrDefault(a => a.Street == addressText);

                if (existingAddress != null)
                {
                    AddressComboBox.ToolTip = "✓ Адресата вече съществува в системата";
                    AddressComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                }
                else
                {
                    AddressComboBox.ToolTip = "○ Ново място";
                    AddressComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                }
            }
            else
            {
                AddressComboBox.ClearValue(ToolTipProperty);
                AddressComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void BlockComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string blockText = BlockComboBox.Text.Trim();
            string addressText = AddressComboBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(blockText) && !string.IsNullOrWhiteSpace(addressText))
            {
                using var context = new SyndiceoDBContext();
                var address = context.Addresses.FirstOrDefault(a => a.Street == addressText);

                if (address != null)
                {
                    var existingBlock = context.Blocks.FirstOrDefault(b =>
                        b.BlockName == blockText && b.AddressId == address.AddressId);

                    if (existingBlock != null)
                    {
                        BlockComboBox.ToolTip = "✓ Блока вече съществува";
                        BlockComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    }
                    else
                    {
                        BlockComboBox.ToolTip = "○ Нов блок";
                        BlockComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                    }
                }
            }
            else
            {
                BlockComboBox.ClearValue(ToolTipProperty);
                BlockComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void EntranceComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string entranceText = CleanEntranceText(EntranceComboBox.Text);
            string blockText = BlockComboBox.Text.Trim();
            string addressText = AddressComboBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(entranceText) && !string.IsNullOrWhiteSpace(blockText))
            {
                using var context = new SyndiceoDBContext();
                var address = context.Addresses.FirstOrDefault(a => a.Street == addressText);

                if (address != null)
                {
                    var block = context.Blocks.FirstOrDefault(b =>
                        b.BlockName == blockText && b.AddressId == address.AddressId);

                    if (block != null)
                    {
                        var existingEntrance = context.Entrances.FirstOrDefault(ent =>
                            ent.EntranceName == entranceText && ent.BlockId == block.BlockId);

                        if (existingEntrance != null)
                        {
                            EntranceComboBox.ToolTip = "✓ Входа вече съществува";
                            EntranceComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                        }
                        else
                        {
                            EntranceComboBox.ToolTip = "○ Нов вход";
                            EntranceComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                        }
                    }
                }
            }
            else
            {
                EntranceComboBox.ClearValue(ToolTipProperty);
                EntranceComboBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void ApartmentNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string aptInput = ApartmentNumberTextBox.Text.Trim();
            string entranceText = CleanEntranceText(EntranceComboBox.Text);
            string blockText = BlockComboBox.Text.Trim();
            string addressText = AddressComboBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(aptInput) && int.TryParse(aptInput, out int apartmentNumber))
            {
                using var context = new SyndiceoDBContext();
                var address = context.Addresses.FirstOrDefault(a => a.Street == addressText);

                if (address != null)
                {
                    var block = context.Blocks.FirstOrDefault(b =>
                        b.BlockName == blockText && b.AddressId == address.AddressId);

                    if (block != null)
                    {
                        var entrance = context.Entrances.FirstOrDefault(ent =>
                            ent.EntranceName == entranceText && ent.BlockId == block.BlockId);

                        if (entrance != null)
                        {
                            var existingApartment = context.Apartments.FirstOrDefault(a =>
                                a.ApartmentNumber == apartmentNumber && a.EntranceId == entrance.EntranceId);

                            if (existingApartment != null)
                            {
                                ApartmentNumberTextBox.ToolTip = "✗ Апартамент вече съществува!";
                                ApartmentNumberTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightCoral);
                            }
                            else
                            {
                                ApartmentNumberTextBox.ToolTip = "○ Нов апартамент";
                                ApartmentNumberTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(aptInput))
            {
                ApartmentNumberTextBox.ToolTip = "⚠ Трябва да е число";
                ApartmentNumberTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightCoral);
            }
            else
            {
                ApartmentNumberTextBox.ClearValue(ToolTipProperty);
                ApartmentNumberTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}