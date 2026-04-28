using Syndiceo.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using static Syndiceo.Windows.ManagementWindow;

namespace Syndiceo.Windows
{
    public partial class ShowApartmentWindow : Window
    {
        private ApartmentViewModel _apartmentViewModel;

        public ShowApartmentWindow(ApartmentViewModel apartmentViewModel)
        {
            InitializeComponent();
            _apartmentViewModel = apartmentViewModel;

            LoadApartmentData();
        }

        private void LoadApartmentData()
        {
            if (_apartmentViewModel == null)
            {
                MessageBox.Show("Апартаментът не е зададен!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            using var context = new SyndiceoDBContext();

            var apartmentEntity = context.Apartments
                .Include(a => a.Entrance)
                    .ThenInclude(e => e.Block)
                        .ThenInclude(b => b.Address)
                .FirstOrDefault(a => a.ApartmentId == _apartmentViewModel.ApartmentId);

            if (apartmentEntity == null)
            {
                MessageBox.Show("Апартаментът не е намерен!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var owner = context.Owners.FirstOrDefault(o => o.ApartmentId == apartmentEntity.ApartmentId);
            var entrance = apartmentEntity.Entrance;
            var block = entrance?.Block;
            var address = block?.Address;

            FullAddressLabel.Text = $"{address?.Street ?? "Няма улица"}, Блок {block?.BlockName ?? "Няма блок"}, Вход {entrance?.EntranceName ?? "Няма вход"}, Апартамент № {apartmentEntity.ApartmentNumber}";

            ownerNameTxtBox.Text = owner?.OwnerName ?? "Няма данни";
            ownerPhoneTxtBox.Text = owner?.PhoneNumber ?? "Няма данни";
            residentCountTxtBox.Text = (apartmentEntity?.ResidentCount ?? 0).ToString();
            NotesTextBox.Text = apartmentEntity.Note ?? "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
      
    }
}
