using Microsoft.SqlServer.Dac;
using Microsoft.Win32;
using Syndiceo.Data.Models;
using Syndiceo.Windows;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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
using System.Xml;
using MessageBox = System.Windows.MessageBox;

namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void AutoSaveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoArchive = true;
            Properties.Settings.Default.Save();
        }

        private void AutoSaveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoArchive = false;
            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
            DatabasePathTextBox.Text = Properties.Settings.Default.LastBackupPath;
            Properties.Settings.Default.isNameChanged = false;
            Properties.Settings.Default.isPasswordChanged = false;
            Properties.Settings.Default.isUsernameChanged = false;
        }

        private void RememberPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberPassword = true;
        }

        private void RememberPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberPassword = false;

        }

        private void RememberUsernameCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberUsername = true;

        }

        private void RememberUsernameCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberUsername = false;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
        }


        private void labelButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ChangeName = true;
            Properties.Settings.Default.Save();
            ChangeUserDataWindow c = new ChangeUserDataWindow();
            c.ShowDialog();
            if (Properties.Settings.Default.isNameChanged)
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        private void usernameButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ChangeUsername = true;
            Properties.Settings.Default.Save();
            ChangeUserDataWindow c = new ChangeUserDataWindow();
            c.ShowDialog();
            if (Properties.Settings.Default.isUsernameChanged)
            {
                AutoCloseMessageBox.Show("Засечена промяна в потребителското име, затваряне на приложението..", 1500);
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        private void emailButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ChangeEmail = true;
            Properties.Settings.Default.Save();
            ChangeUserDataWindow c = new ChangeUserDataWindow();
            c.ShowDialog();
            LoadUserData();
        }

        private void passwordButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ChangePassword = true;
            Properties.Settings.Default.Save();
            ChangeUserDataWindow c = new ChangeUserDataWindow();
            c.ShowDialog();
            if (Properties.Settings.Default.isPasswordChanged)
            {
                AutoCloseMessageBox.Show("Засечена промяна в паролата, затваряне на приложението..", 1500);
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        private void LoadArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Изберете облачен архив (.bacpac)",
                Filter = "Cloud Backup files (*.bacpac)|*.bacpac|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            string bacpacFile = openFileDialog.FileName;

            var result = MessageBox.Show(
                "ВНИМАНИЕ: Облачният импорт ще се опита да създаде базата наново. " +
                "Ако базата вече съществува, операцията може да се провали. Продължавате ли?",
                "Потвърждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            string connectionString = "Server=db45189.public.databaseasp.net; Database=db45189; User Id=db45189; Password=qJ!5@f8Sd9H_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

            RestoreCloudDatabase(connectionString, bacpacFile);
        }
        public void RestoreCloudDatabase(string connectionString, string bacpacFilePath)
        {
            try
            {
                string dbName = "db45189"; 

                if (!File.Exists(bacpacFilePath))
                {
                    MessageBox.Show("Файлът не е намерен!");
                    return;
                }

                MessageBox.Show("Започва възстановяване... Това може да отнеме няколко минути, моля изчакайте.");

                DacServices services = new DacServices(connectionString);

                using (BacPackage package = BacPackage.Load(bacpacFilePath))
                {
                    services.ImportBacpac(package, dbName);
                }

                MessageBox.Show("Базата данни е възстановена успешно от облачния архив!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при зареждането: {ex.Message}\n\n" +
                                "Забележка: Понякога трябва ръчно да изтриете старата база в облака, преди да импортирате нова върху нея.");
            }
        }
        private void LoadUserData()
        {
            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault();

                if (user != null)
                {
                    usernameLabel.Text = user.Username ?? "Няма зададено потребителско име";
                    nameLabel.Text = user.PersonName ?? "Няма зададено име";
                    emailLabel.Text = user.Email ?? "Няма зададен имейл";

                    RememberUsernameCheckBox.IsEnabled = true;
                    RememberPasswordCheckBox.IsEnabled = true;
                    AutoSaveCheckBox.IsEnabled = true;
                    RememberUsernameCheckBox.Opacity = 1;
                    RememberPasswordCheckBox.Opacity = 1;
                    AutoSaveCheckBox.Opacity = 1;

                    RememberUsernameCheckBox.IsChecked = Properties.Settings.Default.RememberUsername;
                    RememberPasswordCheckBox.IsChecked = Properties.Settings.Default.RememberPassword;
                    AutoSaveCheckBox.IsChecked = Properties.Settings.Default.AutoArchive;
                }
            }
        }

    }
}
