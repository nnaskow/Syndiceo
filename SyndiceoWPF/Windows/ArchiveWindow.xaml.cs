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

                string connectionString = "Server=(LocalDB)\\MSSQLLocalDB;Database=SyndiceoDB;Trusted_Connection=True;";
                try
                {
                    BackupDatabase(connectionString, folderPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Грешка при архивирането: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async void BackupDatabase(string connectionString, string backupFolder)
        {
            string dbName = "SyndiceoDB";
            string backupFile = Path.Combine(backupFolder, $"{dbName}_{System.DateTime.Now:yyyyMMdd_HHmm}.bak");

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                conn.Open();
                string sql = $@"BACKUP DATABASE [{dbName}] 
                                TO DISK = N'{backupFile}' 
                                WITH FORMAT, INIT, NAME = N'Backup на базата {dbName}'";
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show($"Архивирането е успешно!\nФайл: {backupFile}");
            if(Properties.Settings.Default.MainWindowClosing==true)
            {
                await Task.Delay(500);
                this.Close();
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
