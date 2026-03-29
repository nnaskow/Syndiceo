using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Syndiceo.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Syndiceo.Windows
{
    public partial class PrintWIndow : Window
    {
        private List<TransactionViewModel> Incomes;
        private List<TransactionViewModel> Expenses;
        private decimal Cashbox;
        private string FullAddress;
        private int EntranceId;
        private string reportsFolder;

        public PrintWIndow(List<TransactionViewModel> incomes, List<TransactionViewModel> expenses, decimal cashbox, int entranceId, string fullAddress)
        {
            InitializeComponent();
            Incomes = incomes;
            Expenses = expenses;
            Cashbox = cashbox;
            EntranceId = entranceId;
            FullAddress = fullAddress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            reportsFolder = Path.Combine(appData, "Syndiceo", "Documents", "MonthlyReports");

            if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);

            FilesListBox.Items.Clear();
            FilesListBox.Items.Add(new ListBoxItem { Content = "Месечен отчет (€)", Tag = "standard" });
            FilesListBox.Items.Add(new ListBoxItem { Content = "Подробен месечен отчет (€)", Tag = "podroben" });
            FilesListBox.Items.Add(new ListBoxItem { Content = "Отчет ремонтни дейности (€)", Tag = "remontni" });
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
            var db = new SyndiceoDBContext();

            var selectedTemplate = FilesListBox.SelectedItem as ListBoxItem;
            if (selectedTemplate == null)
            {
                MessageBox.Show("Моля, изберете шаблон за печат.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isMaintenanceReport = selectedTemplate.Tag?.ToString().Contains("remontni", StringComparison.OrdinalIgnoreCase) ?? false;
            var chooseDateWindow = new ChooseDateWindow { Owner = this };
            if (chooseDateWindow.ShowDialog() != true)
            {
                return;
            }

            string monthStr = Properties.Settings.Default.monthForPrinting;
            string yearStr = Properties.Settings.Default.yearForPrinting;
            string currentPeriod = $"{monthStr}-{yearStr}";

            if (!int.TryParse(monthStr, out int month)) month = DateTime.Now.Month;
            if (!int.TryParse(yearStr, out int year)) year = DateTime.Now.Year;

            if (!isMaintenanceReport)
            {
                bool alreadyArchivedForThisEntrance = db.ApartmentTransactions.Any(at =>
                    at.Apartment.EntranceId == EntranceId &&
                    at.Category.Name == "Несъбрана такса" &&
                    at.TransDate.Month == month &&
                    at.TransDate.Year == year);

                if (Properties.Settings.Default.LastReportDate == currentPeriod || alreadyArchivedForThisEntrance)
                {
                    var recomputeResult = MessageBox.Show(
                        $"За периода {currentPeriod} данните вече са архивирани и таксите са нулирани.\n\n" +
                        "Желаете ли само да генерирате документа (Word) БЕЗ ново преизчисляване?",
                        "Повторен отчет", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);

                    if (recomputeResult == MessageBoxResult.Cancel) return;

                    if (recomputeResult == MessageBoxResult.Yes)
                    {
                        GenerateReport(selectedTemplate);
                        return;
                    }
                }

                var msgbox = MessageBox.Show(
                    $"Сигурни ли сте, че желаете да приключите месеца ({currentPeriod}) и да генерирате отчета?\n" +
                    "(Това ще прехвърли неплатените суми като дълг и ще нулира текущите плащания.)",
                    "Потвърждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (msgbox != MessageBoxResult.Yes) return;
            }

            GenerateReport(selectedTemplate);

            if (!isMaintenanceReport)
            {
                ArchiveMonthData(currentPeriod);
                MessageBox.Show($"Месецът ({currentPeriod}) беше приключен успешно!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void ArchiveMonthData(string currentPeriod)
        {
            using var db = new SyndiceoDBContext();

            var entrance = db.Entrances
                .Include(e => e.Cashboxes)
                .Include(e => e.Apartments)
                    .ThenInclude(a => a.Debts)
                .Include(e => e.Apartments)
                    .ThenInclude(a => a.ApartmentTransactions)
                        .ThenInclude(at => at.Category)
                .Include(e => e.EntranceTransactions)
                    .ThenInclude(et => et.Category)
                .FirstOrDefault(e => e.EntranceId == EntranceId);

            if (entrance == null) return;

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
                db.SaveChanges();
            }

            foreach (var apartment in entrance.Apartments)
            {
                var debt = apartment.Debts.FirstOrDefault();
                if (debt == null) continue;

                decimal totalToCollect = debt.TotalSum;
                decimal alreadyPaid = debt.PaidSum;
                decimal actualMissing = totalToCollect - alreadyPaid;

                if (actualMissing > 0)
                {
                    bool alreadyArchived = db.ApartmentTransactions.Any(at =>
                        at.ApartmentId == apartment.ApartmentId &&
                        at.CategoryId == remainingCategory.Id &&
                        at.TransDate.Month == DateTime.Now.Month &&
                        at.TransDate.Year == DateTime.Now.Year);

                    if (!alreadyArchived)
                    {
                        var tr = new ApartmentTransaction
                        {
                            ApartmentId = apartment.ApartmentId,
                            CategoryId = remainingCategory.Id,
                            Amount = actualMissing,
                            TransDate = DateOnly.FromDateTime(DateTime.Now),
                            Description = $"Несъбрана такса за {DateTime.Now:MMMM yyyy}",
                        };
                        db.ApartmentTransactions.Add(tr);
                    }
                }

                debt.PaidSum = 0;
                debt.RemainingSum = debt.TotalSum;
                apartment.IsMarked = false;
            }

            var cashbox = entrance.Cashboxes.FirstOrDefault() ?? new Cashbox { EntranceId = EntranceId, CurrentBalance = 0 };
            if (cashbox.Id == 0) db.Cashboxes.Add(cashbox);

            decimal totalIncomes = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category?.Kind == "Приход" && et.Category?.Appliance == "entrances")
                .Sum(et => et.Amount);

            decimal totalExpenses = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category?.Kind == "Разход" && et.Category?.Name != "Несъбрана такса" && et.Category?.Appliance == "entrances")
                .Sum(et => Math.Abs(et.Amount));

            decimal currentMonthUncollected = db.ApartmentTransactions.Local
                .Where(at => at.CategoryId == remainingCategory.Id)
                .Sum(at => at.Amount);

            cashbox.CurrentBalance = (cashbox.CurrentBalance + totalIncomes) - totalExpenses - currentMonthUncollected;

            var collectedCategory = db.Categories.FirstOrDefault(c => c.Name == "Събрани такси" && c.Kind == "Приход");
            if (collectedCategory != null)
            {
                var entranceTransactions = db.EntranceTransactions
                    .Where(et => et.EntranceId == EntranceId && et.CategoryId == collectedCategory.Id)
                    .ToList();

                foreach (var et in entranceTransactions) { et.Amount = 0; }
            }

            db.SaveChanges();

            Properties.Settings.Default.LastReportDate = currentPeriod;
            Properties.Settings.Default.Save();

            SessionData.LastPayments.Clear();
        }
        private void GenerateReport(ListBoxItem selectedTemplate)
        {
            string optionTag = selectedTemplate.Tag?.ToString() ?? "standard";
            bool isDetailed = optionTag.Contains("podroben", StringComparison.OrdinalIgnoreCase);
            bool isMaintenanceReport = optionTag.Contains("remontni", StringComparison.OrdinalIgnoreCase);
            string currency = "€";
            int month = int.TryParse(Properties.Settings.Default.monthForPrinting, out int m) ? m : DateTime.Now.Month;
            int year = int.TryParse(Properties.Settings.Default.yearForPrinting, out int y) ? y : DateTime.Now.Year;

            DateTime reportDate = new DateTime(year, month, 1);
            string monthYear = reportDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("bg-BG"));

            using var db = new SyndiceoDBContext();
            var entrance = db.Entrances
                .Include(e => e.Block)
                .ThenInclude(b => b.Address)
                .FirstOrDefault(e => e.EntranceId == EntranceId);

            if (entrance == null)
            {
                MessageBox.Show("Не е намерен вход.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string entranceAddress = $"{entrance.Block?.Address?.Street ?? "Без адрес"}, {entrance.Block?.BlockName ?? "Без блок"}, Вход {entrance.EntranceName}";
            decimal finalBalance = 0;
            if (!isMaintenanceReport)
            {
                var currentCashbox = db.Cashboxes.FirstOrDefault(c => c.EntranceId == EntranceId);
                decimal totalIncomes = db.EntranceTransactions.Where(et => et.EntranceId == EntranceId && et.Amount > 0 && et.Category.Kind == "Приход").Sum(et => et.Amount);
                decimal totalExpenses = db.EntranceTransactions.Where(et => et.EntranceId == EntranceId && et.Amount > 0 && et.Category.Kind == "Разход" && et.Category.Name != "Несъбрана такса").Sum(et => Math.Abs(et.Amount));
                decimal totalUncollected = db.ApartmentTransactions.Where(at => at.Apartment.EntranceId == EntranceId && at.Category.Name == "Несъбрана такса").Sum(at => Math.Abs(at.Amount));
                finalBalance = (currentCashbox?.CurrentBalance ?? 0) + totalIncomes - totalExpenses - totalUncollected;
            }

            string yearFolder = Path.Combine(reportsFolder, year.ToString());
            string monthFolder = Path.Combine(yearFolder, reportDate.ToString("MMMM", System.Globalization.CultureInfo.GetCultureInfo("bg-BG")));
            if (!Directory.Exists(monthFolder)) Directory.CreateDirectory(monthFolder);

            string reportTypeName = isDetailed ? "podroben" : (isMaintenanceReport ? "remontni" : "standart");
            string newFileName = $"МО_{entranceAddress}_{monthYear}_{reportTypeName}_{Guid.NewGuid().ToString().Substring(0, 4)}.docx";
            string newFilePath = Path.Combine(monthFolder, newFileName);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(newFilePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                Body body = mainPart.Document.AppendChild(new Body());

                AddHeader(mainPart, isMaintenanceReport ? "ОТЧЕТ НА ИЗВЪРШЕНИТЕ РЕМОНТНИ ДЕЙНОСТИ" : "МЕСЕЧЕН ОТЧЕТ", isMaintenanceReport);

                body.Append(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new FontSize { Val = "28" }, new Bold()),
                    new Text(isMaintenanceReport ? $"за {year} година" : monthYear))));

                body.Append(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new FontSize { Val = "24" }, new Italic()),
                    new Text(entranceAddress))));

                body.Append(new Paragraph(new Run(new Break())));

                if (isMaintenanceReport)
                {
                    var maintenanceRecords = db.Maintenances.Where(m => m.EntranceId == EntranceId && m.DateOfMaintenance.Year == year).OrderByDescending(m => m.DateOfMaintenance).ToList();
                    if (maintenanceRecords.Count == 0)
                    {
                        body.Append(new Paragraph(new Run(new Text($"Няма въведени ремонтни дейности за {year} г."))));
                    }
                    else
                    {
                        Table table = CreateBaseTable();
                        table.Append(new TableRow(CreateTableCell("Дата", true, "28"), CreateTableCell("Описание", true, "28"), CreateTableCell("Стойност", true, "28")));
                        foreach (var r in maintenanceRecords)
                        {
                            table.Append(new TableRow(
                                CreateTableCell(r.DateOfMaintenance.ToString("dd.MM.yyyy"), false, "26"),
                                CreateTableCell(r.Description ?? "-", false, "26"),
                                CreateTableCell(FormatAmount(r.Price ?? 0, currency), false, "26")
                            ));
                        }
                        body.Append(table);
                    }
                }
                else if (isDetailed)
                {
                    var apartments = db.Apartments.Where(a => a.EntranceId == EntranceId).OrderBy(a => a.ApartmentNumber).ToList();
                    var categories = db.Categories
                        .Where(c => c.Kind == "Разход" && c.Appliance == "apartments")
                        .ToList()
                        .Where(c => {
                            if (c.Name != "Несъбрана такса") return true;
                            return db.ApartmentTransactions.Any(t => t.CategoryId == c.Id && t.Amount != 0);
                        })
                        .OrderBy(c => c.Name)
                        .ToList(); Table table = CreateBaseTable();
                    var hr = new TableRow(CreateTableCell("Ап.", true, "18"), CreateTableCell("Живущи", true, "18"));
                    foreach (var cat in categories) hr.Append(CreateTableCell(cat.Name, true, "18"));
                    hr.Append(CreateTableCell("Общо", true, "18"), CreateTableCell("Подпис", true, "18"));
                    table.Append(hr);

                    foreach (var apt in apartments)
                    {
                        var tr = new TableRow(CreateTableCell(apt.ApartmentNumber.ToString(), false, "22"), CreateTableCell(apt.ResidentCount.ToString(), false, "22"));
                        decimal rowSum = 0;
                        foreach (var cat in categories)
                        {
                            decimal val = db.ApartmentTransactions.Where(t => t.ApartmentId == apt.ApartmentId && t.CategoryId == cat.Id).Sum(t => (decimal?)t.Amount) ?? 0;
                            tr.Append(CreateTableCell(FormatAmount(val, currency), false, "22"));
                            rowSum += val;
                        }
                        tr.Append(CreateTableCell(FormatAmount(rowSum, currency), false, "22"), CreateTableCell("", false, "22"));
                        table.Append(tr);
                    }
                    body.Append(table);
                }
                else
                {
                    bool hasUncollectedIncomes = Incomes.Any(i => i.CategoryName == "Несъбрана такса" && i.Amount != 0);
                    bool hasUncollectedExpenses = Expenses.Any(e => e.CategoryName == "Несъбрана такса" && e.Amount != 0);

                    Table table = CreateBaseTable();

                    table.Append(new TableRow(
                        CreateTableCell("Приходи", true, "32"),
                        CreateTableCell("Стойност", true, "32"),
                        CreateTableCell("Разходи", true, "32"),
                        CreateTableCell("Стойност", true, "32")
                    ));

                    var filteredIncomes = Incomes.Where(i => i.CategoryName != "Несъбрана такса" || i.Amount != 0).ToList();
                    var filteredExpenses = Expenses.Where(e => e.CategoryName != "Несъбрана такса" || e.Amount != 0).ToList();

                    int max = Math.Max(filteredIncomes.Count, filteredExpenses.Count);

                    for (int i = 0; i < max; i++)
                    {
                        table.Append(new TableRow(
                            CreateTableCell(i < filteredIncomes.Count ? filteredIncomes[i].CategoryName : "", false, "28"),
                            CreateTableCell(i < filteredIncomes.Count ? FormatAmount(filteredIncomes[i].Amount, currency) : "", false, "28"),
                            CreateTableCell(i < filteredExpenses.Count ? filteredExpenses[i].CategoryName : "", false, "28"),
                            CreateTableCell(i < filteredExpenses.Count ? FormatAmount(Math.Abs(filteredExpenses[i].Amount), currency) : "", false, "28")
                        ));
                    }

                    body.Append(table);

                    body.Append(new Paragraph(new Run(new Text(""))));
                    Table cashTable = CreateBaseTable();
                    cashTable.Append(new TableRow(
                        CreateTableCell("Наличност в касата:", true, "24"),
                        CreateTableCell(FormatAmount(finalBalance, currency), true, "24"),
                        CreateTableCell("", false, "24"),
                        CreateTableCell("", false, "24")
                    ));
                    body.Append(cashTable);
                }
                AddFooter(mainPart);
                AddPageBorder(mainPart);
                mainPart.Document.Save();
            }
            Process.Start(new ProcessStartInfo { FileName = newFilePath, UseShellExecute = true });
        }

        private void AddHeader(MainDocumentPart mainPart, string title, bool isMaintenance)
        {
            HeaderPart headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();

            var titleParagraph = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new Bold(),
                        new FontSize { Val = "36" },
                        new Color { Val = "000000" },
                        new Underline()
                    ),
                    new Text(title)
                )
            );

            header.Append(titleParagraph);
            headerPart.Header = header;

            var sectionProps = mainPart.Document.Body.Elements<SectionProperties>().LastOrDefault()
                               ?? mainPart.Document.Body.AppendChild(new SectionProperties());

            sectionProps.RemoveAllChildren<HeaderReference>();

            sectionProps.PrependChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) });
        }
        private void AddFooter(MainDocumentPart mainPart)
        {
            FooterPart footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(
                new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                    new Run(new RunProperties(new FontSize { Val = "18" }, new Italic()), new Text($"Отчетът е изготвен с помощта на програмата Syndiceo.©{DateTime.Now.Year} Разработена от NYXON. Всички права запазени"))),
                new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new FontSize { Val = "20" }), new Text("Страница ")),
                    new Run(new SimpleField() { Instruction = " PAGE " }),
                    new Run(new RunProperties(new FontSize { Val = "20" }), new Text(" от ")),
                    new Run(new SimpleField() { Instruction = " NUMPAGES " }))
            );
            var sectionProps = mainPart.Document.Body.Elements<SectionProperties>().LastOrDefault() ?? mainPart.Document.Body.AppendChild(new SectionProperties());
            sectionProps.PrependChild(new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) });
        }

        private Table CreateBaseTable()
        {
            Table table = new Table();
            table.AppendChild(new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "9500" }, // Малко по-тясна
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
            return table;
        }
        private TableCell CreateTableCell(string text, bool isHeader = false, string fontSize = "28")
        {
            var cell = new TableCell();
            cell.Append(new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
            cell.Append(new Paragraph(new ParagraphProperties(new Justification { Val = isHeader ? JustificationValues.Center : JustificationValues.Left }),
                new Run(new RunProperties(new FontSize { Val = fontSize }, isHeader ? new Bold() : null), new Text(text ?? ""))));
            return cell;
        }

        private void RefreshDebtsFromTransactions()
        {
            using var db = new SyndiceoDBContext();

            var debts = db.Debts
                .Include(d => d.Apartment)
                .ToList();

            foreach (var debt in debts)
            {

                debt.PaidSum = 0;
                debt.RemainingSum = debt.TotalSum;
            }

            db.SaveChanges();
        }

        private string FormatAmount(decimal amount, string currency) => currency == "€" ? currency + amount.ToString("F2") : amount.ToString("F2") + " " + currency;
        private void AddPageBorder(MainDocumentPart mainPart)
        {
            var body = mainPart.Document.Body;
            var sectionProps = body.Elements<SectionProperties>().LastOrDefault()
                               ?? body.AppendChild(new SectionProperties());

            var pageBorders = new PageBorders { OffsetFrom = PageBorderOffsetValues.Page };

            uint borderSize = 6;
            uint space = 24;

            pageBorders.AppendChild(new TopBorder { Val = BorderValues.Single, Size = borderSize, Space = space, Color = "000000" });
            pageBorders.AppendChild(new BottomBorder { Val = BorderValues.Single, Size = borderSize, Space = space, Color = "000000" });
            pageBorders.AppendChild(new LeftBorder { Val = BorderValues.Single, Size = borderSize, Space = space, Color = "000000" });
            pageBorders.AppendChild(new RightBorder { Val = BorderValues.Single, Size = borderSize, Space = space, Color = "000000" });

            sectionProps.RemoveAllChildren<PageBorders>();
            sectionProps.AppendChild(pageBorders);
        }
        private void UpdateCashboxBalance(SyndiceoDBContext db, int entranceId)
        {
            var entrance = db.Entrances
                .Include(e => e.Cashboxes)
                .Include(e => e.EntranceTransactions).ThenInclude(et => et.Category)
                .Include(e => e.Apartments).ThenInclude(a => a.ApartmentTransactions).ThenInclude(at => at.Category)
                .FirstOrDefault(e => e.EntranceId == entranceId);

            if (entrance == null) return;

            var cashbox = entrance.Cashboxes.FirstOrDefault();
            if (cashbox == null) return;

            decimal totalIncomes = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category?.Kind == "Приход" && et.Category?.Appliance == "entrances")
                .Sum(et => et.Amount);

            decimal totalExpenses = entrance.EntranceTransactions
                .Where(et => et.Amount > 0 && et.Category?.Kind == "Разход" && et.Category?.Name != "Несъбрана такса" && et.Category?.Appliance == "entrances")
                .Sum(et => Math.Abs(et.Amount));

            decimal totalUncollected = entrance.Apartments
                .SelectMany(a => a.ApartmentTransactions)
                .Where(at => at.Category?.Name == "Несъбрана такса")
                .Sum(at => Math.Abs(at.Amount));

            cashbox.CurrentBalance = (cashbox.CurrentBalance + totalIncomes) - totalExpenses - totalUncollected;

            db.SaveChanges();
        }
    }
}