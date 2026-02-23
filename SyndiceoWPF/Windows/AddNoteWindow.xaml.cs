using Syndiceo.Models;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Syndiceo.Windows
{
    public partial class AddNoteWindow : Window
    {
        private Apartment _selectedApartment;

        public AddNoteWindow(Apartment apartment)
        {
            InitializeComponent();
            _selectedApartment = apartment;

            NoteTextBox.Text = _selectedApartment.Note;
        }
        private void SaveNoteButton_Click(object sender, RoutedEventArgs e)
        {
            string noteText = NoteTextBox.Text;

            if (string.IsNullOrWhiteSpace(noteText))
            {
                MessageBox.Show("Моля, въведете бележка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new SyndiceoDBContext())
                {
                    var apartment = db.Apartments.Find(_selectedApartment.ApartmentId);

                    if (apartment != null)
                    {
                        apartment.Note = noteText;
                        db.SaveChanges();

                        MessageBox.Show("Бележката е запазена успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close(); 
                    }
                    else
                    {
                        MessageBox.Show("Апартаментът не е намерен.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при запазване: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NoteTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
            {

            SaveNoteButton_Click(sender, e);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
     
    }
}
