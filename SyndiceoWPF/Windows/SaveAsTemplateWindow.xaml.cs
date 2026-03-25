using Syndiceo.Data.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using Syndiceo.Utilities;
using Syndiceo.ViewModels;
namespace Syndiceo.Windows
{
    public partial class SaveAsTemplateWindow : Window
    {
        public List<TransactionViewModel> Incomes { get; set; }
        public List<TransactionViewModel> Expenses { get; set; }
        public decimal Cashbox { get; set; }

        public SaveAsTemplateWindow(
            List<TransactionViewModel> incomes,
            List<TransactionViewModel> expenses,
            decimal cashbox,
            int entranceId)
        {
            InitializeComponent();

            // Задаваме стойностите на полетата
            Incomes = incomes;
            Expenses = expenses;
            Cashbox = cashbox;

            SuggestTemplateName();
        }

        private void SuggestTemplateName()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string templatesFolder = Path.Combine(appData, "Syndiceo", "TaxesTemplates");

            if (!Directory.Exists(templatesFolder))
                Directory.CreateDirectory(templatesFolder);

            string baseName = "Template";
            int counter = 1;
            string suggestedName = baseName + counter;

            while (File.Exists(Path.Combine(templatesFolder, suggestedName + ".json")))
            {
                counter++;
                suggestedName = baseName + counter;
            }

            TemplateNameTextBox.Text = suggestedName;
        }

        private void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            string templateName = TemplateNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(templateName))
            {
                MessageBox.Show("Моля, въведете име за шаблона.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string templatesFolder = Path.Combine(appData, "Syndiceo", "TaxesTemplates");

            if (!Directory.Exists(templatesFolder))
                Directory.CreateDirectory(templatesFolder);

            string filePath = Path.Combine(templatesFolder, templateName + ".json");

            int counter = 1;
            string originalName = templateName;
            while (File.Exists(filePath))
            {
                counter++;
                templateName = originalName + counter;
                filePath = Path.Combine(templatesFolder, templateName + ".json");
            }

            var template = new TaxesTemplate
            {
                Name = templateName,
                Incomes = Incomes,
                Expenses = Expenses,
                Cashbox = Cashbox
            };

            File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(template));
            MessageBox.Show($"Шаблонът е запазен успешно като \"{templateName}\"!");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
