using Syndiceo.Data.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml.Packaging;
using OfficeOpenXml;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Syndiceo.Windows
{
    public partial class DocumentsWindow : Window
    {
        private int _selectedEntranceId;

        public DocumentsWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public void LoadAllDocuments()
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
                            : ""
                    })
                    .OrderByDescending(d => d.UploadDate)
                    .ToList();

                AllDocumentsDataGrid.ItemsSource = docs;
            }
        }

        public void LoadEntranceDocuments(int? entranceId = null)
        {
            _selectedEntranceId = entranceId ?? 0;

            using (var context = new SyndiceoDBContext())
            {
                if (entranceId.HasValue && entranceId.Value != 0)
                {
                    EntranceDocumentsDataGrid.ItemsSource = context.Documents
                        .Where(d => d.EntranceId == entranceId.Value)
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
            string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Syndiceo", "Documents");

            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            string targetPath = Path.Combine(targetFolder, fileName);
            File.Copy(sourcePath, targetPath, true);

            using (var context = new SyndiceoDBContext())
            {
                context.Documents.Add(new Document
                {
                    FileName = fileName,
                    FilePath = targetPath,
                    UploadDate = DateTime.Now,
                    EntranceId = _selectedEntranceId
                });
                context.SaveChanges();
            }

            LoadEntranceDocuments(_selectedEntranceId);
            LoadAllDocuments();
        }

        private void ShowExcelPreview(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var package = new ExcelPackage(stream))
                {
                    var ws = package.Workbook.Worksheets.FirstOrDefault();
                    if (ws?.Dimension == null) return;

                    int maxRows = Math.Min(ws.Dimension.End.Row, 50);
                    int maxCols = Math.Min(ws.Dimension.End.Column, 15);

                    for (int row = 1; row <= maxRows; row++)
                    {
                        for (int col = 1; col <= maxCols; col++)
                            sb.Append(ws.Cells[row, col].Text + "\t");
                        sb.AppendLine();
                    }
                }

                PreviewContent.Content = new TextBox
                {
                    Text = sb.ToString(),
                    IsReadOnly = true,
                    FontFamily = new FontFamily("Consolas"),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
            }
            catch (Exception ex)
            {
                PreviewContent.Content = new TextBlock { Text = "Грешка: " + ex.Message, Foreground = Brushes.Red };
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            LoadAllDocuments();
        }

        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem != null)
            {
                string path = dg.SelectedItem is DocumentViewModel vm ? vm.Path : ((Document)dg.SelectedItem).FilePath;
                ShowPreview(path);
            }
        }

        private void ShowPreview(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".xls" || ext == ".xlsx") ShowExcelPreview(filePath);
            else if (ext == ".jpg" || ext == ".png") PreviewContent.Content = new Image { Source = new BitmapImage(new Uri(filePath)) };
            else if (ext == ".txt") PreviewContent.Content = new TextBox { Text = File.ReadAllText(filePath), IsReadOnly = true };
            else PreviewContent.Content = new TextBlock { Text = "Няма предварителен изглед." };
        }

        public void SetAddPanelEnabled(bool enabled)
        {
            addDocsPanel.IsEnabled = enabled;
            addDocsPanel.Opacity = enabled ? 1.0 : 0.35;
        }

        private void OpenAllDocumentsFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Syndiceo", "Documents");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
    }

    public class DocumentViewModel
    {
        public string FileName { get; set; }
        public string FullAddress { get; set; }
        public DateTime UploadDate { get; set; }
        public string Path { get; set; }
    }
}