using Syndiceo.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using MessageBox = System.Windows.MessageBox;
using Syndiceo.Data.Models;
using Syndiceo.Data;
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
                else
                {
                    RememberUsernameCheckBox.IsChecked = false;
                    RememberPasswordCheckBox.IsChecked = false;
                    AutoSaveCheckBox.IsChecked = false;

                    RememberUsernameCheckBox.IsEnabled = false;
                    RememberPasswordCheckBox.IsEnabled = false;
                    AutoSaveCheckBox.IsEnabled = false;

                    RememberUsernameCheckBox.Opacity = 0.375;
                    RememberPasswordCheckBox.Opacity = 0.375;
                    AutoSaveCheckBox.Opacity = 0.375;
                }
            }
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
            c.Show();
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
                Title = "Изберете архив на базата данни",
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            string backupFile = openFileDialog.FileName;

            var result = MessageBox.Show(
                "Сигурни ли сте, че искате да възстановите базата от този архив? Всички текущи данни ще бъдат презаписани!",
                "Потвърждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;";

                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();

                    string restoreSql = $@"
                ALTER DATABASE [SyndiceoDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [SyndiceoDB] FROM DISK = N'{backupFile}' WITH REPLACE;
                ALTER DATABASE [SyndiceoDB] SET MULTI_USER;
            ";

                    using (var command = new System.Data.SqlClient.SqlCommand(restoreSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Базата беше успешно възстановена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при възстановяване: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       
    }
}
