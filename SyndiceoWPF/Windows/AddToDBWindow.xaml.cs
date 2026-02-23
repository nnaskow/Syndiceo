
using Microsoft.EntityFrameworkCore;
using Syndiceo.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Syndiceo.Windows
{
    public partial class AddToDBWindow : Window
    {
        public AddToDBWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string addressText = AddressComboBox.Text.Trim();
            string blockText = BlockComboBox.Text.Trim();
            string entranceText = EntranceComboBox.Text.Trim();
            int apartmentText = int.Parse(ApartmentNumberTextBox.Text);
            string ownerName = ownerNameTxtBox.Text.Trim();
            string ownerPhone = ownerPhoneNumberTxtBox.Text.Trim();
            string noteText = NoteTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(addressText))
            {
                MessageBox.Show("Моля въведете поне адрес!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new SyndiceoDBContext())
            {
                // --- Адрес ---
                var address = context.Addresses.FirstOrDefault(a => a.Street == addressText);
                if (address == null)
                {
                    address = new Address { Street = addressText };
                    context.Add(address);
                    context.SaveChanges();
                }

                // --- Блок ---
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

                // --- Вход ---
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

                // --- Апартамент ---
                // --- Апартамент ---
                Apartment apartment = null;
                if ( apartmentText !=null && entrance != null)
                {
                    apartment = context.Apartments
                        .FirstOrDefault(a => a.ApartmentNumber == apartmentText && a.EntranceId == entrance.EntranceId);

                    if (apartment == null)
                    {
                        // безопасно парсване на броя живущи
                        int residentCount = 0;
                        if (!string.IsNullOrWhiteSpace(ResidentsCountTextBox.Text))
                        {
                            if (!int.TryParse(ResidentsCountTextBox.Text, out residentCount))
                            {
                                MessageBox.Show("Моля, въведете валиден брой живущи (число).",
                                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            if (residentCount < 0)
                            {
                                MessageBox.Show("Броят живущи не може да бъде отрицателен.",
                                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        apartment = new Apartment
                        {
                            ApartmentNumber = apartmentText,
                            Note = noteText,
                            IsMarked = false,
                            EntranceId = entrance.EntranceId,
                            ResidentCount = residentCount
                        };

                        context.Apartments.Add(apartment);
                        context.SaveChanges();
                    }
                }

                // --- Собственик ---
                if (!string.IsNullOrWhiteSpace(ownerName) && !string.IsNullOrWhiteSpace(ownerPhone) && apartment != null)
                {
                    var ownerExists = context.Owners.Any(o => o.ApartmentId == apartment.ApartmentId &&
                                                               (o.OwnerName == ownerName || o.PhoneNumber == ownerPhone));
                    if (!ownerExists)
                    {
                        var owner = new Owner
                        {
                            OwnerName = ownerName,
                            PhoneNumber = ownerPhone,
                            ApartmentId = apartment.ApartmentId
                        };
                        context.Owners.Add(owner);
                        context.SaveChanges();
                    }
                }
            }

            // --- Изчистване на полетата ---
            AddressComboBox.Text = "";
            BlockComboBox.Text = "";
            EntranceComboBox.Text = "";
            ApartmentNumberTextBox.Clear();
            ownerNameTxtBox.Clear();
            ownerPhoneNumberTxtBox.Clear();
            NoteTextBox.Clear();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
      
    }
}
