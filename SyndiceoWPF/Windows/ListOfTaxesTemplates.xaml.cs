using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Syndiceo.Windows
{
    public partial class ListOfTaxesTemplates : Window
    {
        public TaxesTemplate SelectedTemplate { get; private set; }

        public ListOfTaxesTemplates()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string templatesFolder = Path.Combine(appData, "Syndiceo", "TaxesTemplates");

                if (!Directory.Exists(templatesFolder))
                    Directory.CreateDirectory(templatesFolder);

                var files = Directory.GetFiles(templatesFolder, "*.json")
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .OrderBy(f => f)
                                     .ToList();

                FilesListBox.ItemsSource = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при зареждане на шаблоните:\n{ex.Message}",
                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem == null)
            {
                MessageBox.Show("Моля, изберете шаблон от списъка.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string selectedName = FilesListBox.SelectedItem.ToString();
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string templatesFolder = Path.Combine(appData, "Syndiceo", "TaxesTemplates");
                string filePath = Path.Combine(templatesFolder, selectedName + ".json");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Избраният шаблон не съществува.");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var template = JsonSerializer.Deserialize<TaxesTemplate>(json);

                if (template == null)
                {
                    MessageBox.Show("Грешка при зареждане на шаблона.");
                    return;
                }

                SelectedTemplate = template;
                DialogResult = true; // затваря прозореца и връща резултата
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при отваряне на шаблона:\n{ex.Message}",
                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
