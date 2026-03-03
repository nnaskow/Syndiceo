using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Syndiceo.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Syndiceo.Data.Models;
using Syndiceo.Data;
namespace Syndiceo.Windows
{
    public partial class DocumentsWindow : Window
    {
        private int _selectedEntranceId;

        public DocumentsWindow()
        {
            InitializeComponent();
        }
        public void LoadEntranceDocuments(int? entranceId = null)
        {
            _selectedEntranceId = entranceId ?? 0;

            using (var context = new SyndiceoDBContext())
            {
                var query = context.Documents
                    .Include(d => d.Entrance)
                        .ThenInclude(e => e.Block)
                            .ThenInclude(b => b.Address)
                    .AsQueryable();

                if (entranceId.HasValue && entranceId.Value != 0)
                {
                    query = query.Where(d => d.EntranceId == entranceId.Value);
                    EntranceDocumentsDataGrid.ItemsSource = query
                        .OrderByDescending(d => d.UploadDate)
                        .ToList();
                }
                else
                {
                    EntranceDocumentsDataGrid.ItemsSource = null;
                }

             
            }

            SetAddPanelEnabled(entranceId.HasValue && entranceId.Value != 0);
        }


        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() != true) return;

            string sourcePath = dialog.FileName;
            string fileName = Path.GetFileName(sourcePath);

            string targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Syndiceo", "Documents");

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            string targetPath = Path.Combine(targetFolder, fileName);
            File.Copy(sourcePath, targetPath, true);

            var doc = new Document
            {
                FileName = fileName,
                FilePath = targetPath,
                UploadDate = DateTime.Now,
                EntranceId = _selectedEntranceId
            };

            using (var context = new SyndiceoDBContext())
            {
                context.Documents.Add(doc);
                context.SaveChanges();
            }

            LoadEntranceDocuments(_selectedEntranceId);
        }

        private void OpenAllDocumentsFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                         "Syndiceo", "Documents");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is DocumentViewModel doc)
            {
                ShowPreview(doc.Path);
            }
        }

        private void ShowPreview(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            try
            {
                if (ext == ".jpg" || ext == ".png" || ext == ".bmp")
                {
                    PreviewContent.Content = new Image
                    {
                        Source = new BitmapImage(new Uri(filePath)),
                        Stretch = Stretch.Uniform
                    };
                }
                else if (ext == ".txt")
                {
                    PreviewContent.Content = new TextBox
                    {
                        Text = File.ReadAllText(filePath),
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                }
                else if (ext == ".pdf")
                {
                    var browser = new WebBrowser();
                    browser.Navigate(new Uri(filePath));
                    PreviewContent.Content = browser;
                }
                else if (ext == ".doc" || ext == ".docx")
                {
                    string textContent = "";
                    try
                    {
                        using (var docx = WordprocessingDocument.Open(filePath, false))
                        {
                            textContent = docx.MainDocumentPart.Document.Body.InnerText;
                        }
                    }
                    catch
                    {
                        textContent = "Недостъпен е изгледът в Word документ.";
                    }

                    PreviewContent.Content = new TextBox
                    {
                        Text = textContent,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                }
                else if (ext == ".xls" || ext == ".xlsx")
                {
                    ShowExcelPreview(filePath);
                }
                else
                {
                    PreviewContent.Content = new TextBlock
                    {
                        Text = "Бързият изглед не е достъпен за този формат.",
                        Foreground = Brushes.Gray,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
            catch
            {
                PreviewContent.Content = new TextBlock
                {
                    Text = "Грешка при зареждане на бързия изглед.",
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        public void SetAddPanelEnabled(bool enabled)
        {
            addDocsPanel.IsEnabled = enabled;
            addDocsPanel.Opacity = enabled ? 1.0 : 0.35;
        }

        private void ShowExcelPreview(string filePath)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var sb = new StringBuilder();
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets.FirstOrDefault();
                    if (ws == null)
                    {
                        PreviewContent.Content = new TextBlock
                        {
                            Text = "Няма намерени работни листове в този Excel файл.",
                            Foreground = Brushes.Gray,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        return;
                    }

                    if (ws.Dimension == null)
                    {
                        PreviewContent.Content = new TextBlock
                        {
                            Text = "Листът е празен.",
                            Foreground = Brushes.Gray,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        return;
                    }

                    int maxRows = Math.Min(ws.Dimension.End.Row, 50);
                    int maxCols = Math.Min(ws.Dimension.End.Column, 20);

                    for (int row = ws.Dimension.Start.Row; row <= maxRows; row++)
                    {
                        for (int col = ws.Dimension.Start.Column; col <= maxCols; col++)
                        {
                            sb.Append(ws.Cells[row, col].Text + "\t");
                        }
                        sb.AppendLine();
                    }
                }

                PreviewContent.Content = new TextBox
                {
                    Text = sb.ToString(),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas")
                };
            }
            catch (Exception ex)
            {
                PreviewContent.Content = new TextBlock
                {
                    Text = "Грешка при зареждане на Excel: " + ex.Message,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }


        private string docsFolder;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                var docs = context.Documents
                    .Include(d => d.Entrance)
                        .ThenInclude(e => e.Block)
                            .ThenInclude(b => b.Address)
                    .Select(d => new DocumentViewModel
                    {
                        FileName = d.FileName,
                        Path = d.FilePath,
                        UploadDate = d.UploadDate,
                        FullAddress = d.Entrance != null
                            ? $"{d.Entrance.Block.Address.Street} {d.Entrance.Block.BlockName}, Вход {d.Entrance.EntranceName}"
                            : "" // ако документът не е вързан за вход
                    })
                    .OrderByDescending(d => d.UploadDate)
                    .ToList();

                AllDocumentsDataGrid.ItemsSource = docs;
            }
        }

    }
}
public class DocumentViewModel
{
    public string FileName { get; set; }       // Името на файла
    public string FullAddress { get; set; }    // Адрес/Вход (ако има такъв)
    public DateTime UploadDate { get; set; }   // Дата на качване/създаване
    public string Path { get; set; }           // Пълен път до файла
}

