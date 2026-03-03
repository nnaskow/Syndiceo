
using Microsoft.IdentityModel.Tokens;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Syndiceo.Models;
using static Syndiceo.Windows.ManagementWindow;
using Syndiceo.Data.Models;
using Syndiceo.Data;
namespace Syndiceo.Windows
{
    public partial class EditWindow : Window
    {
        private AddressViewModel _addressVM;
        private BlockViewModel _blockVM;
        private EntranceViewModel _entranceVM;
        private ApartmentViewModel _apartmentVM;

        public EditWindow(
            AddressViewModel address = null,
            BlockViewModel block = null,
            EntranceViewModel entrance = null,
            ApartmentViewModel apartment = null)
        {
            InitializeComponent();

            _addressVM = address;
            _blockVM = block;
            _entranceVM = entrance;
            _apartmentVM = apartment;

            LoadAndSetEditable();
        }

        private void SetReadOnly(TextBox tb)
        {
            tb.IsReadOnly = true;
            tb.Background = Brushes.LightGray;
        }

        private void SetEditable(TextBox tb)
        {
            tb.IsReadOnly = false;
            tb.Background = Brushes.White;
        }

        private void LoadAndSetEditable()
        {
            AdressTextBox.Text = _addressVM?.Street ?? "";
            BlockTextBox.Text = _blockVM?.BlockName ?? "";
            EntranceTextBox.Text = _entranceVM?.Name ?? "";
            ApartmentNumberTextBox.Text = _apartmentVM?.ApartmentNumber.ToString() ?? string.Empty;
            ownerNameTxtBox.Text = _apartmentVM?.OwnerName ?? "";
            ownerPhoneNumberTxtBox.Text = _apartmentVM?.OwnerPhone ?? "";
            ResidentsCountTextBox.Text = _apartmentVM?.ResidentCount.ToString() ?? "";

            SetReadOnly(ResidentsCountTextBox);
            SetReadOnly(AdressTextBox);
            SetReadOnly(BlockTextBox);
            SetReadOnly(EntranceTextBox);
            SetReadOnly(ApartmentNumberTextBox);
            SetReadOnly(ownerNameTxtBox);
            SetReadOnly(ownerPhoneNumberTxtBox);

            if (_apartmentVM != null)
            {
                SetEditable(ApartmentNumberTextBox);
                SetEditable(ownerNameTxtBox);
                SetEditable(ownerPhoneNumberTxtBox);
                SetEditable(ResidentsCountTextBox);
            }
            else if (_entranceVM != null)
            {
                SetEditable(EntranceTextBox);
            }
            else if (_blockVM != null)
            {
                SetEditable(BlockTextBox);
            }
            else if (_addressVM != null)
            {
                SetEditable(AdressTextBox);
            }
        }


        private void SavePropertyButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                if (_apartmentVM != null)
                {
                    _apartmentVM.ApartmentNumber = int.Parse(ApartmentNumberTextBox.Text);
                    _apartmentVM.OwnerName = ownerNameTxtBox.Text.Trim();
                    _apartmentVM.OwnerPhone = ownerPhoneNumberTxtBox.Text.Trim();
                    if (int.TryParse(ResidentsCountTextBox.Text.Trim(), out int count))
                        _apartmentVM.ResidentCount = count;

                    var apartment = context.Apartments.FirstOrDefault(a => a.ApartmentId == _apartmentVM.ApartmentId);
                    if (apartment != null)
                    {
                        apartment.ApartmentNumber = _apartmentVM.ApartmentNumber;
                        apartment.ResidentCount = _apartmentVM.ResidentCount;

                        string? nameText = string.IsNullOrWhiteSpace(ownerNameTxtBox.Text) || ownerNameTxtBox.Text == "Няма данни"
      ? null
      : ownerNameTxtBox.Text.Trim();

                        string? phoneText = string.IsNullOrWhiteSpace(ownerPhoneNumberTxtBox.Text) || ownerPhoneNumberTxtBox.Text == "Няма данни"
                            ? null
                            : ownerPhoneNumberTxtBox.Text.Trim();

                        var owner = context.Owners.FirstOrDefault(o => o.ApartmentId == apartment.ApartmentId);

                        if (owner != null)
                        {
                            owner.OwnerName = nameText;
                            owner.PhoneNumber = phoneText;
                        }
                        else
                        {
                            context.Owners.Add(new Owner
                            {
                                ApartmentId = apartment.ApartmentId,
                                OwnerName = nameText,
                                PhoneNumber = phoneText
                            });
                        }
                        context.SaveChanges();

                        context.SaveChanges();
                    }
                }
                else if (_entranceVM != null)
                {
                    _entranceVM.Name = EntranceTextBox.Text.Trim();
                    var entrance = context.Entrances.FirstOrDefault(e => e.EntranceId == _entranceVM.Id);
                    if (entrance != null)
                    {
                        entrance.EntranceName = _entranceVM.Name;
                        context.SaveChanges();
                    }
                }
                else if (_blockVM != null)
                {
                    _blockVM.BlockName = BlockTextBox.Text.Trim();
                    var block = context.Blocks.FirstOrDefault(b => b.BlockId == _blockVM.Id);
                    if (block != null)
                    {
                        block.BlockName = _blockVM.BlockName;
                        context.SaveChanges();
                    }
                }
                else if (_addressVM != null)
                {
                    _addressVM.Street = AdressTextBox.Text.Trim();
                    var address = context.Addresses.FirstOrDefault(a => a.AddressId == _addressVM.Id);
                    if (address != null)
                    {
                        address.Street = _addressVM.Street;
                        context.SaveChanges();
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }



        private void AdressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }

        private void BlockTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }

        private void EntranceTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }

        private void ApartmentNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }

        private void ownerNameTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }

        private void ownerPhoneNumberTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }
        private void ResidentsCountTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SavePropertyButton_Click(sender, e);
        }


        private void editOwnerButton_Click(object sender, RoutedEventArgs e)
        {
            SaveOwner(_apartmentVM);
        }
        private void SaveOwner(ApartmentViewModel apartment)
        {
            if (apartment == null)
                return;

            string nameText = string.IsNullOrWhiteSpace(ownerNameTxtBox.Text) || ownerNameTxtBox.Text == "Няма данни"
                ? "Няма данни"
                : ownerNameTxtBox.Text.Trim();

            string phoneText = string.IsNullOrWhiteSpace(ownerPhoneNumberTxtBox.Text) || ownerPhoneNumberTxtBox.Text == "Няма данни"
                ? "Няма данни"
                : ownerPhoneNumberTxtBox.Text.Trim();

            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    var owner = context.Owners.FirstOrDefault(o => o.ApartmentId == apartment.ApartmentId);

                    if (owner != null)
                    {
                        owner.OwnerName = nameText;
                        owner.PhoneNumber = phoneText;
                    }
                    else
                    {
                        owner = new Owner
                        {
                            ApartmentId = apartment.ApartmentId,
                            OwnerName = nameText,
                            PhoneNumber = phoneText
                        };
                        context.Owners.Add(owner);
                    }

                    context.SaveChanges();
                }

                apartment.OwnerName = nameText;
                apartment.OwnerPhone = phoneText;

                MessageBox.Show("Данните за собственика бяха запазени успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Възникна грешка при запазването на данните за собственика:\n{ex.Message}",
                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ownerNameTxtBox.Text = string.IsNullOrWhiteSpace(_apartmentVM?.OwnerName)
                ? "Няма данни"
                : _apartmentVM.OwnerName;

            ownerPhoneNumberTxtBox.Text = string.IsNullOrWhiteSpace(_apartmentVM?.OwnerPhone)
                ? "Няма данни"
                : _apartmentVM.OwnerPhone;

            ResidentsCountTextBox.Text = _apartmentVM != null
                ? _apartmentVM.ResidentCount.ToString()
                : "";
        }

    }
}
