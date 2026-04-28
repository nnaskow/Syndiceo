using Microsoft.SqlServer.Dac;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Syndiceo.Windows
{
    public partial class ArchiveWindow : Window
    {
        public ArchiveWindow()
        {
            InitializeComponent();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false, 
                FileName = "Изберете тази папка"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                ArchivePathTextBox.Text = folderPath;

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    MessageBox.Show("Моля, изберете папка за архив.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string connectionString = "Server=db45189.public.databaseasp.net; Database=db45189; User Id=db45189; Password=qJ!5@f8Sd9H_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
                try
                {
                    BackupCloudDatabase(connectionString, folderPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Грешка при архивирането: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async void BackupDatabase(string connectionString, string backupFolder)
        {
            try
            {
                string dbName = "db45189";
                string backupFile = Path.Combine(backupFolder, $"{dbName}_{DateTime.Now:yyyyMMdd_HHmm}.bak");

                using (var conn = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await conn.OpenAsync(); 

                    string sql = $@"BACKUP DATABASE [{dbName}] 
                            TO DISK = N'{backupFile}' 
                            WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                    using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show($"Архивирането е успешно!\nФайл: {backupFile}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при комуникация със сървъра: {ex.Message}\n\n" +
                    "ЗАБЕЛЕЖКА: Ако базата е в облака, не можете да записвате архив директно на вашия диск чрез SQL команда.",
                    "Критична грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (Properties.Settings.Default.MainWindowClosing == true)
            {
                this.Close();
            }
        }
        public void BackupCloudDatabase(string connectionString, string backupFolder)
        {
            try
            {
                string dbName = "db45189";
                string filePath = Path.Combine(backupFolder, $"{dbName}_{DateTime.Now:yyyyMMdd}.bacpac");

                DacServices services = new DacServices(connectionString);

                services.ExportBacpac(filePath, dbName);

                MessageBox.Show($"Архивът (BACPAC) е изтеглен успешно!\nПът: {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при теглене на архива: " + ex.Message);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (Properties.Settings.Default.MainWindowClosing == true)
            {
                archiveLabel.Text = "Желаете ли да направите архив на външен носител?";
            }
        }
        
    }
}
