using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Syndiceo.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;

namespace Syndiceo.Windows
{
    public partial class PrintWIndow : Window
    {
        private List<TransactionViewModel> Incomes;
        private List<TransactionViewModel> Expenses;
        private decimal Cashbox;
        private int EntranceId;
        private string reportsFolder;

        private const string Primary = "1A365D";
        private const string HeaderBlue = "1A365D";
        private const string LightGray = "F9FAFB";
        private const string TextDark = "111827";
        private const string TextMuted = "6B7280";
        private const string RowAlt = "EEF2F7";
        private const string BorderColor = "D1D5DB";
        private const string TotalIncome = "EBF5EB";
        private const string TotalExpense = "FDF2F2";
        private const string DividerBlue = "1A365D";

        public PrintWIndow(List<TransactionViewModel> incomes, List<TransactionViewModel> expenses, decimal cashbox, int entranceId, string fullAddress)
        {
            InitializeComponent();

            Incomes = incomes ?? new List<TransactionViewModel>();
            Expenses = expenses ?? new List<TransactionViewModel>();
            Cashbox = cashbox;
            EntranceId = entranceId;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            reportsFolder = Path.Combine(appData, "Syndiceo", "Documents", "MonthlyReports");

            if (!Directory.Exists(reportsFolder))
                Directory.CreateDirectory(reportsFolder);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilesListBox.Items.Clear();
            FilesListBox.Items.Add(new ListBoxItem { Content = "📊 Месечен отчет (Стандартен)", Tag = "standard" });
            FilesListBox.Items.Add(new ListBoxItem { Content = "✍️ Подробен месечен отчет (Ведомост)", Tag = "podroben" });
            FilesListBox.Items.Add(new ListBoxItem { Content = "🛠️ Годишен отчет ремонтни дейности", Tag = "remontni" });
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(reportsFolder))
                Process.Start(new ProcessStartInfo { FileName = reportsFolder, UseShellExecute = true });
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedTemplate = FilesListBox.SelectedItem as ListBoxItem;
            if (selectedTemplate == null)
            {
                MessageBox.Show("Моля, изберете тип отчет.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tag = selectedTemplate.Tag.ToString();

            var chooseDateWindow = new ChooseDateWindow { Owner = this };
            if (chooseDateWindow.ShowDialog() != true) return;

            int month = int.TryParse(Properties.Settings.Default.monthForPrinting, out int m) ? m : DateTime.Now.Month;
            int year = int.TryParse(Properties.Settings.Default.yearForPrinting, out int y) ? y : DateTime.Now.Year;

            try
            {
                GenerateReport(tag, month, year);

                if (tag == "standard" || tag == "podroben")
                {
                    var result = MessageBox.Show($"Желаете ли да приключите официално месец {month}/{year}?\n\n" +
                        "Това ще нулира 'Платена сума' за всички апартаменти и ще прехвърли неплатеното в историята.",
                        "Приключване на месец", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ArchiveMonthAndResetDebts(month, year);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при генериране на отчета:\n{ex.Message}",
                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArchiveMonthAndResetDebts(int month, int year)
        {
            try
            {
                using var db = new SyndiceoDBContext();

                var collCat = db.Categories.FirstOrDefault(c => c.Name == "Събрани такси");
                var uncollCat = db.Categories.FirstOrDefault(c => c.Name == "Несъбрана такса");

                if (collCat == null || uncollCat == null)
                {
                    MessageBox.Show("Грешка: Липсват необходимите категории в базата данни.");
                    return;
                }

                var cb = db.Cashboxes.FirstOrDefault(c => c.EntranceId == EntranceId);
                if (cb == null) return;

                var debts = db.Debts.Include(d => d.Apartment)
                                    .Where(d => d.Apartment.EntranceId == EntranceId)
                                    .ToList();

                decimal totalIncomeFromApartments = debts.Sum(d => d.PaidSum);
                decimal totalExp = Expenses.Sum(e => Math.Abs(e.Amount));

                cb.CurrentBalance += (totalIncomeFromApartments - totalExp);

                foreach (var debt in debts)
                {
                    decimal finalBalanceToCarryForward = debt.TotalSum - debt.PaidSum;

                    var transactionsToClear = db.ApartmentTransactions
                        .Where(at => at.ApartmentId == debt.ApartmentId &&
                                    (at.CategoryId == uncollCat.Id || at.CategoryId == collCat.Id))
                        .ToList();

                    db.ApartmentTransactions.RemoveRange(transactionsToClear);

                    if (finalBalanceToCarryForward > 0)
                    {
                        db.ApartmentTransactions.Add(new ApartmentTransaction
                        {
                            ApartmentId = debt.ApartmentId ?? 0,
                            CategoryId = uncollCat.Id,
                            Amount = finalBalanceToCarryForward,
                            TransDate = DateOnly.FromDateTime(DateTime.Now),
                            Description = $"Остатък към {month}/{year}"
                        });
                    }

                    debt.PaidSum = 0;
                }

                var entranceIncomeToReset = db.EntranceTransactions
                    .Where(et => et.EntranceId == EntranceId && et.CategoryId == collCat.Id)
                    .ToList();
                db.EntranceTransactions.RemoveRange(entranceIncomeToReset);

                db.SaveChanges();

                MessageBox.Show($"Месец {month}/{year} е приключен успешно!\n\n" +
                                $"Касата е обновена.\n" +
                                $"Неплатените суми са прехвърлени като нов дълг.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? ex.InnerException.Message : "Няма";
                MessageBox.Show($"Критична грешка при запис в базата:\n{ex.Message}\n\nДетайли: {inner}",
                                "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateReport(string type, int month, int year)
        {
            string targetPath = Path.Combine(reportsFolder, year.ToString(), month.ToString("D2"));
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

            using var db = new SyndiceoDBContext();
            var entrance = db.Entrances
                .Include(e => e.Block).ThenInclude(b => b.Address)
                .FirstOrDefault(e => e.EntranceId == EntranceId);

            string cleanEntrance = Regex.Replace(entrance?.EntranceName ?? "", @"(?i)\b(вход|вх\.|вх)\b", "").Trim();

            string addressInfo = $"ул. {entrance?.Block?.Address?.Street} №{entrance?.Block?.BlockName}, вх. {cleanEntrance}";
            string fileName = $"МО_{addressInfo}_{DateTime.Now:yyyyMMdd_HHmm_ss}.docx";
            string fullPath = Path.Combine(targetPath, fileName);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(fullPath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                Body body = mainPart.Document.AppendChild(new Body());

                string mainTitle = type == "remontni" ? "ОТЧЕТ ЗА РЕМОНТНИ ДЕЙНОСТИ" : (type == "podroben" ? "ПОДРОБЕН МЕСЕЧЕН ОТЧЕТ (ВЕДОМОСТ)" : "МЕСЕЧЕН ОТЧЕТ (СТАНДАРТЕН)");
                AddHeader(mainPart, mainTitle);

                string periodStr = type == "remontni" ? $"ЗА {year} ГОДИНА" : new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("bg-BG")).ToUpper();
                body.Append(CreateStyledParagraph(periodStr, "32", Primary, true));
                body.Append(CreateStyledParagraph(addressInfo, "20", "555555", false));
                body.Append(new Paragraph(new Run(new Break())));

                if (type == "remontni") BuildMaintenanceTable(body, db, year);
                else if (type == "podroben") BuildDetailedTable(body, db);
                else BuildStandardTable(body, db);

                AddFooter(mainPart);
                AddPageBorder(mainPart);
                mainPart.Document.Save();
            }

            Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
        }

        private void BuildStandardTable(Body body, SyndiceoDBContext db)
        {
            decimal totalInc = Incomes?.Sum(i => i.Amount) ?? 0;
            decimal totalExp = Expenses?.Sum(e => Math.Abs(e.Amount)) ?? 0;
            decimal startCashbox = Cashbox;
            decimal endCashbox = startCashbox + totalInc - totalExp;

            AddModernSummary(body, "НАЧАЛНО САЛДО", $"€{startCashbox:F2}", Primary);

            Table balanceTable = CreateBaseTable();

            balanceTable.AppendChild(new TableGrid(
                new GridColumn { Width = "3400" },
                new GridColumn { Width = "1400" },
                new GridColumn { Width = "3400" },
                new GridColumn { Width = "1800" }
            ));

            balanceTable.Append(new TableRow(
                CreateTableCell("ПРИХОДИ", true, "20", HeaderBlue, isHeader: true, isAmount: false),
                CreateTableCell("СУМА", true, "20", HeaderBlue, isHeader: true, isAmount: true),
                CreateTableCell("РАЗХОДИ", true, "20", HeaderBlue, isHeader: true, isAmount: false, leftDivider: true),
                CreateTableCell("СУМА", true, "20", HeaderBlue, isHeader: true, isAmount: true)
            ));

            var incomesToDisplay = Incomes ?? new List<TransactionViewModel>();
            var expensesToDisplay = Expenses ?? new List<TransactionViewModel>();

            int maxRows = Math.Max(incomesToDisplay.Count, expensesToDisplay.Count);

            for (int i = 0; i < maxRows; i++)
            {
                var row = new TableRow();
                string bg = (i % 2 == 0) ? "FFFFFF" : RowAlt;

                if (i < incomesToDisplay.Count)
                {
                    var income = incomesToDisplay[i];
                    string incomeName = GetTransactionName(income);

                    row.Append(CreateTableCell(incomeName, false, "20", bg));
                    row.Append(CreateTableCell($"{income.Amount:F2}", false, "20", bg, isAmount: true));
                }
                else
                {
                    row.Append(CreateTableCell("", false, "20", bg));
                    row.Append(CreateTableCell("", false, "20", bg));
                }

                if (i < expensesToDisplay.Count)
                {
                    var expense = expensesToDisplay[i];
                    row.Append(CreateTableCell(expense.CategoryName ?? "Без категория", false, "20", bg, leftDivider: true));
                    row.Append(CreateTableCell($"{Math.Abs(expense.Amount):F2}", false, "20", bg, isAmount: true));
                }
                else
                {
                    row.Append(CreateTableCell("", false, "20", bg, leftDivider: true));
                    row.Append(CreateTableCell("", false, "20", bg));
                }

                balanceTable.Append(row);
            }

            balanceTable.Append(new TableRow(
                CreateTableCell("ОБЩО ПРИХОДИ:", true, "20", TotalIncome),
                CreateTableCell($"{totalInc:F2}", true, "20", TotalIncome, isAmount: true),
                CreateTableCell("ОБЩО РАЗХОДИ:", true, "20", TotalExpense, leftDivider: true),
                CreateTableCell($"{totalExp:F2}", true, "20", TotalExpense, isAmount: true)
            ));

            body.Append(balanceTable);
            body.Append(new Paragraph(new Run(new Break())));
            AddModernSummary(body, "КРАЙНА НАЛИЧНОСТ", $"€{endCashbox:F2}", Primary);
        }

        private string GetTransactionName(TransactionViewModel transaction)
        {
            if (!string.IsNullOrWhiteSpace(transaction.CategoryName))
                return transaction.CategoryName;

            if (!string.IsNullOrWhiteSpace(transaction.Description))
                return transaction.Description;

            var nameProperty = transaction.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                var nameValue = nameProperty.GetValue(transaction)?.ToString();
                if (!string.IsNullOrWhiteSpace(nameValue))
                    return nameValue;
            }

            return "Неизвестна транзакция";
        }

        private void BuildDetailedTable(Body body, SyndiceoDBContext db)
        {
            Table table = CreateBaseTable();

            var categories = db.ApartmentTransactions
                .Where(at => at.Apartment.EntranceId == EntranceId && at.Category.Name != "Събрани такси")
                .Select(at => at.Category.Name)
                .Distinct()
                .ToList();

            if (!categories.Any())
            {
                body.Append(CreateStyledParagraph("Няма данни за разход по апартаменти", "20", TextMuted, false));
                return;
            }

            TableRow headerRow = new TableRow();
            headerRow.Append(CreateTableCell("Ап.", true, "20", HeaderBlue, isHeader: true));
            headerRow.Append(CreateTableCell("Живущи", true, "20", HeaderBlue, isHeader: true));
            foreach (var catName in categories)
                headerRow.Append(CreateTableCell(catName, true, "18", HeaderBlue, isHeader: true, isAmount: true));
            headerRow.Append(CreateTableCell("Общо", true, "20", HeaderBlue, isHeader: true, isAmount: true));
            headerRow.Append(CreateTableCell("Подпис", true, "20", HeaderBlue, isHeader: true));
            table.Append(headerRow);

            var apps = db.Apartments
                .Include(a => a.ApartmentTransactions).ThenInclude(at => at.Category)
                .Where(a => a.EntranceId == EntranceId)
                .OrderBy(a => a.ApartmentNumber).ToList();

            if (!apps.Any())
            {
                body.Append(CreateStyledParagraph("Няма апартаменти за този вход", "20", TextMuted, false));
                return;
            }

            for (int i = 0; i < apps.Count; i++)
            {
                var app = apps[i];
                string bg = (i % 2 == 0) ? "FFFFFF" : RowAlt;
                decimal rowTotal = 0;

                TableRow dataRow = new TableRow();
                dataRow.Append(CreateTableCell(app.ApartmentNumber.ToString(), true, "20", bg));
                dataRow.Append(CreateTableCell(app.ResidentCount.ToString(), false, "20", bg));

                foreach (var catName in categories)
                {
                    var amount = app.ApartmentTransactions
                        .Where(at => at.Category.Name == catName)
                        .Sum(at => at.Amount);
                    rowTotal += amount;
                    dataRow.Append(CreateTableCell(amount > 0 ? $"{amount:F2}" : "-", false, "20", bg, isAmount: true));
                }

                dataRow.Append(CreateTableCell($"{rowTotal:F2}", true, "20", bg, isAmount: true));
                dataRow.Append(CreateTableCell("", false, "20", bg));
                table.Append(dataRow);
            }

            body.Append(table);
        }

        private void BuildMaintenanceTable(Body body, SyndiceoDBContext db, int year)
        {
            Table table = CreateBaseTable();

            table.Append(new TableRow(
                CreateTableCell("ДАТА", true, "20", HeaderBlue, isHeader: true),
                CreateTableCell("ОПИСАНИЕ", true, "20", HeaderBlue, isHeader: true),
                CreateTableCell("СУМА", true, "20", HeaderBlue, isHeader: true, isAmount: true)
            ));

            var repairs = db.Maintenances
                .Where(m => m.EntranceId == EntranceId && m.DateOfMaintenance.Year == year)
                .OrderBy(m => m.DateOfMaintenance).ToList();

            if (!repairs.Any())
            {
                body.Append(CreateStyledParagraph($"Няма ремонти за година {year}", "20", TextMuted, false));
                return;
            }

            decimal sum = 0;
            for (int i = 0; i < repairs.Count; i++)
            {
                string bg = (i % 2 == 0) ? "FFFFFF" : RowAlt;
                decimal price = (decimal)(repairs[i].Price ?? 0);
                sum += price;

                table.Append(new TableRow(
                    CreateTableCell(repairs[i].DateOfMaintenance.ToString("dd.MM.yyyy"), false, "20", bg),
                    CreateTableCell(repairs[i].Description, false, "20", bg),
                    CreateTableCell($"{price:F2} €", false, "20", bg, isAmount: true)
                ));
            }

            body.Append(table);
            body.Append(new Paragraph(new Run(new Break())));
            body.Append(CreateStyledParagraph($"ОБЩО ЗА ГОДИНАТА: {sum:F2} €", "22", Primary, true));
        }

        private Table CreateBaseTable()
        {
            Table table = new Table();
            table.AppendChild(new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "10000" },
                new TableJustification { Val = TableRowAlignmentValues.Center },
                new TableBorders(
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new LeftBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 2, Color = BorderColor },
                    new InsideVerticalBorder { Val = BorderValues.None }
                )
            ));
            return table;
        }

        private TableCell CreateTableCell(
            string text,
            bool isBold,
            string fontSize,
            string fill,
            bool isHeader = false,
            bool isAmount = false,
            bool leftDivider = false)
        {
            text = text ?? "";

            var cellProps = new TableCellProperties(
                new Shading { Fill = fill ?? "FFFFFF" },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                new TableCellMargin(
                    new TopMargin { Width = "180", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "180", Type = TableWidthUnitValues.Dxa },
                    new LeftMargin { Width = "200", Type = TableWidthUnitValues.Dxa },
                    new RightMargin { Width = "200", Type = TableWidthUnitValues.Dxa }
                )
            );

            var borders = new TableCellBorders(
                new BottomBorder { Val = BorderValues.Single, Size = 2, Color = BorderColor }
            );
            if (leftDivider)
                borders.Append(new LeftBorder { Val = BorderValues.Single, Size = 6, Color = DividerBlue });

            cellProps.Append(borders);

            var cell = new TableCell(cellProps);

            string finalFontSize = text.Length > 20
                ? (int.Parse(fontSize) - 4).ToString()
                : fontSize;

            var pProps = new ParagraphProperties(
                new Justification { Val = isAmount ? JustificationValues.Right : JustificationValues.Left }
            );
            pProps.Append(new SpacingBetweenLines { Before = "60", After = "60" });

            string textColor = isHeader ? "FFFFFF" : (TextDark ?? "111827");

            var runProps = new RunProperties(
                new RunFonts { Ascii = "Verdana" },
                new FontSize { Val = finalFontSize },
                new Color { Val = textColor }
            );
            if (isBold) runProps.Append(new Bold());

            cell.AppendChild(new Paragraph(pProps, new Run(runProps, new Text(text))));
            return cell;
        }

        private Paragraph CreateStyledParagraph(string text, string fontSize, string color, bool isBold)
        {
            var p = new Paragraph(new ParagraphProperties(
                new Justification { Val = JustificationValues.Center }
            ));
            var runProps = new RunProperties(
                new FontSize { Val = fontSize },
                new Color { Val = color },
                new RunFonts { Ascii = "Verdana" }
            );
            if (isBold) runProps.Append(new Bold());
            p.AppendChild(new Run(runProps, new Text(text)));
            return p;
        }

        private void AddModernSummary(Body body, string title, string amount, string color)
        {
            Table table = new Table();
            table.AppendChild(new TableProperties(
                new TableJustification { Val = TableRowAlignmentValues.Left },
                new TableBorders(
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None },
                    new InsideHorizontalBorder { Val = BorderValues.None },
                    new InsideVerticalBorder { Val = BorderValues.None }
                )
            ));

            TableCell cell = new TableCell(new TableCellProperties());

            Paragraph pTitle = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Left }));
            pTitle.AppendChild(new Run(new RunProperties(
                new FontSize { Val = "16" },
                new RunFonts { Ascii = "Verdana" },
                new Color { Val = "888888" }),
                new Text(title.ToUpper())));
            cell.AppendChild(pTitle);

            Paragraph pAmount = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Left }));
            pAmount.AppendChild(new Run(new RunProperties(
                new Bold(),
                new FontSize { Val = "38" },
                new RunFonts { Ascii = "Verdana" },
                new Color { Val = color }),
                new Text(amount)));
            cell.AppendChild(pAmount);

            table.Append(new TableRow(cell));
            body.Append(table);
        }

        private void AddHeader(MainDocumentPart mainPart, string title)
        {
            HeaderPart hp = mainPart.AddNewPart<HeaderPart>();

            hp.Header = new Header(
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new FontSize { Val = "28" },
                            new Color { Val = TextDark }
                        ),
                        new Text(title)
                    )
                )
            );

            var sectionProps = mainPart.Document.Body.Elements<SectionProperties>().LastOrDefault()
                ?? mainPart.Document.Body.AppendChild(new SectionProperties());

            sectionProps.PrependChild(new HeaderReference
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(hp)
            });
        }

        private void AddFooter(MainDocumentPart mainPart)
        {
            FooterPart fp = mainPart.AddNewPart<FooterPart>();

            fp.Footer = new Footer(
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(
                        new RunProperties(
                            new FontSize { Val = "12" },
                            new Color { Val = TextMuted }
                        ),
                        new Text($"Създадено с Syndiceo • {DateTime.Now:dd.MM.yyyy HH:mm}")
                    )
                )
            );

            var sectionProps = mainPart.Document.Body.Elements<SectionProperties>().LastOrDefault()
                ?? mainPart.Document.Body.AppendChild(new SectionProperties());

            sectionProps.PrependChild(new FooterReference
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(fp)
            });
        }

        private void AddPageBorder(MainDocumentPart mainPart)
        {
            var sectionProps = mainPart.Document.Body.Elements<SectionProperties>().LastOrDefault() ?? mainPart.Document.Body.AppendChild(new SectionProperties());
            var borders = new PageBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = Primary },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = Primary },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = Primary },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = Primary }
            )
            { OffsetFrom = PageBorderOffsetValues.Page };
            sectionProps.Append(borders);
        }
    }
}