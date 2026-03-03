using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Syndiceo.Models;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Utilities;
using Syndiceo.Data.Models;
using Syndiceo.Data;
namespace Syndiceo.Windows
{
    public partial class PrintWIndow : Window
    {
        private List<TransactionViewModel> Incomes;
        private List<TransactionViewModel> Expenses;
        private decimal Cashbox;
        private string FullAddress;
        private int EntranceId;

        public PrintWIndow(List<TransactionViewModel> incomes, List<TransactionViewModel> expenses, decimal cashbox, int entranceId, string fullAddress)
        {
            InitializeComponent();
            Incomes = incomes;
            Expenses = expenses;
            Cashbox = cashbox;
            EntranceId = entranceId;
            FullAddress = fullAddress;
        }

        private string templatesFolder;
        private string reportsFolder;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            templatesFolder = Path.Combine(appData, "Syndiceo", "Documents", "Templates");
            reportsFolder = Path.Combine(appData, "Syndiceo", "Documents", "MonthlyReports");

            if (!Directory.Exists(templatesFolder)) Directory.CreateDirectory(templatesFolder);
            if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);

            FilesListBox.Items.Clear();
            foreach (var file in Directory.GetFiles(templatesFolder))
            {
                FilesListBox.Items.Add(new ListBoxItem
                {
                    Content = Path.GetFileName(file),
                    Tag = file
                });
            }
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = reportsFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            var msgbox = MessageBox.Show("Сигурни ли сте, че желаете да генерирате отчета?\n\nПрепоръчваме ви да прегледате таксите отново.",
    "Потвърждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(msgbox != MessageBoxResult.Yes)
            {
                return;
            }
            using var db = new SyndiceoDBContext();
            var selectedTemplate = FilesListBox.SelectedItem as ListBoxItem;
            if (selectedTemplate == null)
            {
                MessageBox.Show("Моля, изберете шаблон за печат.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isMaintenanceReport = selectedTemplate.Content.ToString().Contains("remontni", StringComparison.OrdinalIgnoreCase);

            if (!isMaintenanceReport)
            {
                var chooseDateWindow = new ChooseDateWindow();
                chooseDateWindow.Owner = this;

                bool? dialogResult = chooseDateWindow.ShowDialog();

                if (dialogResult != true) 
                {
                    MessageBox.Show("Отчетът не беше генериран, защото не е избрана дата.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(Properties.Settings.Default.monthForPrinting) ||
                    string.IsNullOrEmpty(Properties.Settings.Default.yearForPrinting))
                {
                    MessageBox.Show("Не сте избрали месец или година за отчета.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            GenerateReport(selectedTemplate);

            var entrance1 = db.Entrances
                .Include(e => e.Apartments)
                    .ThenInclude(a => a.ApartmentTransactions)
                .FirstOrDefault(e => e.EntranceId == EntranceId);

            if (entrance1 != null)
            {
                var remainingCategory = db.Categories.FirstOrDefault(c => c.Name == "Несъбрана такса");

                if (remainingCategory == null)
                {
                    remainingCategory = new Syndiceo.Data.Models.Category
                    {
                        Name = "Несъбрана такса",
                        Kind = "Разход",
                        Appliance = "apartments"
                    };
                    db.Categories.Add(remainingCategory);
                }
                else
                {
                    remainingCategory.Kind = "Разход";
                    remainingCategory.Appliance = "apartments";
                }

                db.SaveChanges();

                foreach (var apartment in entrance1.Apartments)
                {
                    var debts = db.Debts.Where(d => d.ApartmentId == apartment.ApartmentId).ToList();

                    decimal remainingTotal = debts.Sum(d => d.RemainingSum ?? 0);

                    if (remainingTotal > 0)
                    {
                        var tr = new ApartmentTransaction
                        {
                            ApartmentId = apartment.ApartmentId,
                            CategoryId = remainingCategory.Id,
                            Amount = remainingTotal,
                            Description = $"Невзета сума за {DateTime.Now:MMMM yyyy}",

                        };

                        db.ApartmentTransactions.Add(tr);
                    }
                }
                var collectedCategory = db.Categories.FirstOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход");
                if (collectedCategory != null)
                {
                    var entranceTransactions = db.EntranceTransactions
                        .Where(et => et.EntranceId == EntranceId && et.CategoryId == collectedCategory.Id)
                        .ToList();

                    foreach (var et in entranceTransactions)
                    {
                        et.Amount = 0;
                    }
                }
                db.SaveChanges();


                var apartmentIds = entrance1.Apartments
                    .Select(a => a.ApartmentId)
                    .ToList();

                var debtsToReset = db.Debts
                    .Where(d => apartmentIds.Contains(d.ApartmentId ?? 0))
                    .ToList();

                foreach (var debt in debtsToReset)
                {
                    if (debt.RemainingSum > 0)
                    {
                        debt.TotalSum = debt.PaidSum; 
                    }
                }
                foreach (var apartment in entrance1.Apartments)
                {
                    apartment.IsMarked = false;
                }
                db.SaveChanges();
                Properties.Settings.Default.isReportDone = true;
                RefreshDebtsFromTransactions();

                using (var dbReset = new SyndiceoDBContext())
                {
                    var taxesWindow = new TaxesWindow();

                    foreach (var ap in entrance1.Apartments)
                    {
                        taxesWindow.UpdateDebtForApartment(ap.ApartmentId);
                    }

                    var apartments = dbReset.Apartments.ToList();
                    foreach (var apt in apartments)
                    {
                        apt.IsMarked = false;
                    }
                    SessionData.LastPayments.Clear();
                    dbReset.SaveChanges();
                }

            }
        }
        private void GenerateReport(ListBoxItem selectedTemplate)
        {
            string templatePath = selectedTemplate.Tag.ToString();
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Файлът не съществува!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isDetailed = selectedTemplate.Content.ToString().Contains("podroben", StringComparison.OrdinalIgnoreCase);
            bool isMaintenanceReport = selectedTemplate.Content.ToString().Contains("remontni", StringComparison.OrdinalIgnoreCase);

            string currency = templatePath.ToLower().Contains("_eur") ? "€" : "лв";

            int month = 1;
            int year = DateTime.Now.Year;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.monthForPrinting))
                month = int.Parse(Properties.Settings.Default.monthForPrinting);

            if (!string.IsNullOrEmpty(Properties.Settings.Default.yearForPrinting))
                year = int.Parse(Properties.Settings.Default.yearForPrinting);

            DateTime reportDate = new DateTime(year, month, 1);
            string monthYear = reportDate.ToString("MMMM yyyy");

            using var db = new SyndiceoDBContext();
            var entrance = db.Entrances
                .Include(e => e.Block.Address)
                .Include(e => e.Cashboxes)
                .Include(e => e.EntranceTransactions)
                .Include(e => e.Apartments)
                    .ThenInclude(a => a.ApartmentTransactions)
                    .Include(e => e.EntranceTransactions)
    .ThenInclude(et => et.Category)
.Include(e => e.Apartments)
    .ThenInclude(a => a.ApartmentTransactions)
        .ThenInclude(at => at.Category)
                .FirstOrDefault(e => e.EntranceId == EntranceId);

            if (entrance == null)
            {
                MessageBox.Show("Не може да се намери входът!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var cashbox = entrance.Cashboxes.FirstOrDefault();
            if (cashbox == null)
            {
                cashbox = new Cashbox
                {
                    EntranceId = entrance.EntranceId,
                    CurrentBalance = 0
                };
                db.Cashboxes.Add(cashbox);
                db.SaveChanges(); 
            }

            decimal currentCash = cashbox.CurrentBalance;

            decimal totalIncomes = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category != null && et.Category.Kind == "Приход" && et.Category.Appliance=="entrances")
                .Sum(et => et.Amount);

            decimal totalExpenses = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category != null
                             && et.Category.Kind == "Разход"
                             && et.Category.Name != "Несъбрана такса" && et.Category.Appliance == "entrances")
                .Sum(et => Math.Abs(et.Amount));

            decimal totalUncollected = 
                entrance.Apartments.SelectMany(a => a.ApartmentTransactions)
                    .Where(at => at.Category != null && at.Category.Name == "Несъбрана такса")
                    .Sum(at => Math.Abs(at.Amount));

            cashbox.CurrentBalance = currentCash + totalIncomes - totalExpenses - totalUncollected;
            db.SaveChanges();


            currentCash = cashbox.CurrentBalance;

            string entranceAddress = $"{entrance.Block.Address.Street}, Блок {entrance.Block.BlockName}, Вход {entrance.EntranceName}";
            string reportType = isDetailed ? "подробен" : "обикновен";
            string newFileName = $"МО_{entrance.Block.Address.Street}_{entrance.Block.BlockName}_Вход {entrance.EntranceName}_{monthYear}_{reportType}.docx";

            string yearFolder = Path.Combine(reportsFolder, year.ToString());
            string monthFolder = Path.Combine(yearFolder, reportDate.ToString("MMMM", System.Globalization.CultureInfo.GetCultureInfo("bg-BG")));
            if (!Directory.Exists(monthFolder))
                Directory.CreateDirectory(monthFolder);

            string newFilePath = Path.Combine(monthFolder, newFileName);
            if (File.Exists(newFilePath))
            {
                try { File.Delete(newFilePath); }
                catch (IOException)
                {
                    MessageBox.Show("Файлът е отворен в Word. Моля, затворете го и опитайте отново.",
                        "Грешка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            File.Copy(templatePath, newFilePath, true);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(newFilePath, true))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;

                string headerText = isMaintenanceReport ? $"за годината {year}" : monthYear;
                body.Append(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new FontSize { Val = "32" }, new Bold()), new Text(headerText))));

                body.Append(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new FontSize { Val = "28" }, new Italic()), new Text(entranceAddress))));
                body.Append(new Paragraph(new Run(new Text(" "))));
                body.Append(new Paragraph(new Run(new Text(" "))));

                body.Append(new Paragraph(new Run(new Text(" "))));
                body.Append(new Paragraph(new Run(new Text(" "))));
                if (isMaintenanceReport)
                {
                    var maintenanceRecords = db.Maintenances
                        .Where(m => m.EntranceId == EntranceId)
                        .OrderByDescending(m => m.DateOfMaintenance)
                        .ToList();

                    if (maintenanceRecords.Count == 0)
                    {
                        body.Append(new Paragraph(
                            new Run(new RunProperties(new FontSize { Val = "28" }),
                            new Text("Няма въведени ремонтни дейности за този вход."))));
                    }
                    else
                    {
                        var table = new Table();
                        table.AppendChild(new TableProperties(
                            new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "10206" },
                            new TableJustification { Val = TableRowAlignmentValues.Center },
                            new TableBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 4 },
                                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                                new RightBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                            )
                        ));

                        var headerRow = new TableRow();
                        headerRow.Append(CreateTableCell("Дата", true, "28"));
                        headerRow.Append(CreateTableCell("Описание", true, "28"));
                        headerRow.Append(CreateTableCell("Стойност", true, "28"));
                        table.Append(headerRow);

                        decimal total = 0m;
                        foreach (var record in maintenanceRecords)
                        {
                            var tr = new TableRow();
                            tr.Append(CreateTableCell(record.DateOfMaintenance.ToString("dd.MM.yyyy"), false, "26"));
                            tr.Append(CreateTableCell(record.Description ?? "-", false, "26"));
                            tr.Append(CreateTableCell(FormatAmount(record.Price ?? 0, currency), false, "26"));
                            total += record.Price ?? 0;
                            table.Append(tr);
                        }

                        var totalRow = new TableRow();
                        totalRow.Append(CreateTableCell("", false, "26"));
                        totalRow.Append(CreateTableCell("Общо:", true, "26"));
                        totalRow.Append(CreateTableCell(FormatAmount(total, currency), true, "26"));
                        table.Append(totalRow);

                        body.Append(table);
                    }
                }
                else
                {


                    if (isDetailed)
                    {
                        var apartments = db.Apartments
                                           .Include(a => a.Entrance.Block.Address)
                                           .Where(a => a.EntranceId == EntranceId)
                                           .ToList();

                        var categories = db.Categories
                                           .Where(c => c.Kind == "Разход")
                                           .OrderBy(c => c.Kind).ThenBy(c => c.Name)
                                           .ToList();

                        var table = new Table();

                        table.AppendChild(new TableProperties(
                            new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "10206" }, // 17,5 см
                            new TableJustification { Val = TableRowAlignmentValues.Center },
                            new TableBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 4 },
                                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                                new RightBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                            )
                        ));

                        var headerRow = new TableRow();
                        headerRow.Append(CreateTableCell("Ап.", true, "18"));
                        headerRow.Append(CreateTableCell("Бр. живущи", true, "18"));
                        foreach (var cat in categories)
                            headerRow.Append(CreateTableCell(cat.Name, true, "18"));
                        headerRow.Append(CreateTableCell("Общо", true, "18"));
                        headerRow.Append(CreateTableCell("Подпис", true, "18"));
                        table.Append(headerRow);

                        foreach (var apt in apartments)
                        {
                            var tr = new TableRow();
                            tr.Append(CreateTableCell(apt.ApartmentNumber.ToString(), false, "22")); // 11pt
                            tr.Append(CreateTableCell(apt.ResidentCount.ToString(), false, "22"));
                            decimal sumTotal = 0m;
                            foreach (var cat in categories)
                            {
                                decimal catSum = db.ApartmentTransactions
                                                   .Where(t => t.ApartmentId == apt.ApartmentId && t.CategoryId == cat.Id)
                                                   .Sum(t => (decimal?)t.Amount) ?? 0m;
                                tr.Append(CreateTableCell(FormatAmount(catSum, currency), false, "22"));
                                sumTotal += catSum;
                            }
                            tr.Append(CreateTableCell(FormatAmount(sumTotal, currency), false, "22"));
                            tr.Append(CreateTableCell("", false, "22"));
                            table.Append(tr);
                        }

                        body.Append(table);
                    }
                    else
                    {
                        var table = new Table();
                        table.AppendChild(new TableProperties(
                            new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "10206" },
                            new TableJustification { Val = TableRowAlignmentValues.Center },
                            new TableBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 4 },
                                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                                new RightBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                            )
                        ));

                        var headerRow = new TableRow();
                        headerRow.Append(CreateTableCell("Приходи", true, "36"));
                        headerRow.Append(CreateTableCell("Стойност", true, "36"));
                        headerRow.Append(CreateTableCell("Разходи", true, "36"));
                        headerRow.Append(CreateTableCell("Стойност", true, "36"));
                        table.Append(headerRow);

                        int maxRows = Math.Max(Incomes.Count, Expenses.Count);
                        for (int i = 0; i < maxRows; i++)
                        {
                            var tr = new TableRow();
                            string incomeCat = i < Incomes.Count ? Incomes[i].CategoryName : "";
                            string incomeVal = i < Incomes.Count ? FormatAmount(Incomes[i].Amount, currency) : "";
                            string expCat = i < Expenses.Count ? Expenses[i].CategoryName : "";
                            string expVal = i < Expenses.Count ? FormatAmount(Math.Abs(Expenses[i].Amount), currency) : "";

                            tr.Append(
                                CreateTableCell(incomeCat, false, "36"),
                                CreateTableCell(incomeVal, false, "36"),
                                CreateTableCell(expCat, false, "36"),
                                CreateTableCell(expVal, false, "36")
                            );
                            table.Append(tr);
                        }
                        body.Append(table);
                    }

                    var tableCashbox = new Table();
                    tableCashbox.AppendChild(new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "10206" },
                        new TableJustification { Val = TableRowAlignmentValues.Center },
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    ));
                    var trCash = new TableRow();
                    trCash.Append(
                        CreateTableCell("Наличност:", true, "24"),
                        CreateTableCell(FormatAmount(currentCash, currency), false, "24"),
                        CreateTableCell("", false, "24"),
                        CreateTableCell("", false, "24")
                    );
                    tableCashbox.Append(trCash);
                    body.Append(tableCashbox);

                    wordDoc.MainDocumentPart.Document.Save();
                }
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = newFilePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при отваряне на Word: " + ex.Message);
            }
        }


        private void RefreshDebtsFromTransactions()
        {
            using var db = new SyndiceoDBContext();

            var debts = db.Debts
                .Include(d => d.Apartment)
                .ToList();

            foreach (var debt in debts)
            {
                var paidFromTransactions = db.ApartmentTransactions
                    .Where(t => t.ApartmentId == debt.ApartmentId && t.Amount > 0)
                    .Sum(t => (decimal?)t.Amount) ?? 0m;

                debt.PaidSum = paidFromTransactions;
                debt.RemainingSum = debt.TotalSum - debt.PaidSum;
                if (debt.RemainingSum < 0) debt.RemainingSum = 0;
            }

            db.SaveChanges();
        }

        private TableCell CreateTableCell(string text, bool isHeader = false, string fontSize = "28")
        {
            var cell = new TableCell();

            var paragraph = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new FontSize { Val = fontSize }, isHeader ? new Bold() : null),
                        new Text(text ?? ""))
            );

            cell.Append(new TableCellProperties(
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            ));

            cell.Append(paragraph);
            return cell;
        }

        private string FormatAmount(decimal amount, string currency)
        {
            return currency == "€" ? currency + amount.ToString("F2") : amount.ToString("F2") + " " + currency;
        }
    }
}
