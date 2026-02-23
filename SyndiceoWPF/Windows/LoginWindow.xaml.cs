using Syndiceo.Models;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DocumentFormat.OpenXml.Math;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using System.Data.SqlClient;
using System.IO;

namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

          /*  if(Properties.Settings.Default.updateDatabase == true)
            {
                AutoCloseMessageBox.Show("Обновяване на базата данни", 5000);
                UpdateDatabaseFromSqlFile();
            }
            Properties.Settings.Default.updateDatabase = false;
            Properties.Settings.Default.Save();*/
            EnsureLocalDBInstalled();
            EnsureDatabaseIsCreated();

            using (var context = new SyndiceoDBContext())
            {
                bool hasAnyLogin = context.Logins.Any();

                if (hasAnyLogin)
                {
                    LoginGrid.Visibility = Visibility.Visible;
                    RegisterGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    LoginGrid.Visibility = Visibility.Hidden;
                    RegisterGrid.Visibility = Visibility.Visible;
                    nameTxtBox.Focus();
                    return;
                }

                var rememberedUser = context.Logins.FirstOrDefault(u => u.RememberMe);

                if (rememberedUser != null)
                {
                    remembermeCheckbox.IsChecked = rememberedUser.RememberMe;

                    bool rememberUsername = Properties.Settings.Default.RememberUsername;
                    bool rememberPassword = Properties.Settings.Default.RememberPassword;

                    if (rememberUsername)
                        usernameTxtBox.Text = rememberedUser.Username;

                    if (rememberPassword)
                        passwordBox.Password = rememberedUser.Password;

                    if (rememberUsername && rememberPassword)
                    {
                        Keyboard.ClearFocus();
                    }
                    else if (rememberUsername && !rememberPassword)
                    {
                        passwordBox.Focus();
                    }
                    else if (!rememberUsername && rememberPassword)
                    {
                        usernameTxtBox.Focus();
                    }
                    else
                    {
                        usernameTxtBox.Focus();
                    }
                }
                else
                {
                    remembermeCheckbox.IsChecked = false;
                    usernameTxtBox.Focus();
                }
            }
            Properties.Settings.Default.Save();
        }
/*        private void UpdateDatabaseFromSqlFile()
        {
            string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=master;Trusted_Connection=True;";
            string sqlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Dependencies", "migration_update.sql");

            if (!File.Exists(sqlFilePath))
            {
                MessageBox.Show("⚠️ SQL файлът за миграции не е намерен: " + sqlFilePath);
                return;
            }

            try
            {
                string script = File.ReadAllText(sqlFilePath);

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Разделяне на скрипта на batch-ове по GO
                    var commands = script.Split(new[] { "\r\nGO\r\n", "\nGO\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var command in commands)
                    {
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            using (var cmd = new SqlCommand(command, connection))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("✅ Базата данни е успешно обновена!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠️ Грешка при обновяване на базата: " + ex.Message, "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/
        private void EnsureDatabaseIsCreated()
        {
            try
            {
                using (var context = new SyndiceoDBContext())
                {                   
                    context.Database.EnsureCreated();                                
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠️ Грешка при обновяване на базата: " + ex.Message,
                    "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void EnsureLocalDBInstalled()
        {
            bool installed = false;
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\15.0"))
            {
                installed = key != null;
            }

            if (!installed)
            {
                try
                {
                    string installerPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Dependencies",
                        "SqlLocalDB.msi");

                    if (!System.IO.File.Exists(installerPath))
                    {
                        MessageBox.Show("Инсталаторът за SQL LocalDB липсва: " + installerPath,
                            "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return;
                    }

                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i \"{installerPath}\" /qn IACCEPTSQLLOCALDBLICENSETERMS=YES",
                        Verb = "runas",
                        UseShellExecute = true
                    };

                    var process = System.Diagnostics.Process.Start(psi);
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        MessageBox.Show("Инсталацията на LocalDB се провали. Код: " + process.ExitCode,
                            "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Грешка при инсталиране на LocalDB: " + ex.Message,
                        "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
        }



        public void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            passwordTextVisible.Text = passwordBox.Password;
            passwordTextVisible.Visibility = Visibility.Visible;
            passwordBox.Visibility = Visibility.Hidden;
        }
        public void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordTextVisible.Text = passwordBox.Password;
            passwordTextVisible.Visibility = Visibility.Hidden;
            passwordBox.Visibility = Visibility.Visible;
        }
        public void ShowPasswordReg_Checked(object sender, RoutedEventArgs e)
        {
            passwordTextVisibleReg.Text = passwordTextBoxReg.Password;
            passwordTextVisibleReg.Visibility = Visibility.Visible;
            passwordTextBoxReg.Visibility = Visibility.Hidden;
        }
        public void ShowPasswordReg_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordTextVisibleReg.Text = passwordTextBoxReg.Password;
            passwordTextVisibleReg.Visibility = Visibility.Hidden;
            passwordTextBoxReg.Visibility = Visibility.Visible;
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTxtBoxReg.Text;
            string password = passwordTextBoxReg.Password;
            string personName = nameTxtBox.Text;
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Моля, попълнете полето 'Потребителско име' ");
                return;
            }
            else if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Моля, попъленете полето 'Парола' ");
                return;
            }
            else if (string.IsNullOrWhiteSpace(personName))
            {
                MessageBox.Show("Моля, попълнете вашето име ");
                return;
            }

            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    if (context.Logins.Any(l => l.Username == username))
                    {
                        MessageBox.Show("Този потребител вече съществува.");
                        return;
                    }

                    var login = new Login
                    {
                        Username = username,
                        Password = passwordHash,
                        PersonName = personName
                    };

                    context.Logins.Add(login);
                    context.SaveChanges();
                }
                new AutoCloseMessageBox($"Успешна регистрация! Добре дошли,{username}!", 5000).ShowDialog();
                RegisterGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при регистрация: " + ex.Message);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTxtBox.Text;
            string password = passwordBox.Password;

            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins
                                  .FirstOrDefault(u => u.Username == username);

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    foreach (var u in context.Logins)
                    {
                        u.RememberMe = false;
                    }

                    if (remembermeCheckbox.IsChecked == true)
                    {
                        user.RememberMe = true;
                    }

                    context.SaveChanges();

                    var existingWindow = Application.Current.Windows
                                                    .OfType<ManagementWindow>()
                                                    .FirstOrDefault();

                    if (existingWindow == null)
                    {
                        ManagementWindow management = new ManagementWindow();
                        management.Show();
                    }
                    else
                    {
                        MessageBox.Show("Вече сте логнати в Syndiceo.",
                                        "Информация",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);

                        existingWindow.Activate();
                    }

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Грешно потребителско име или парола!");
                }
            }


        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void usernameTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                passwordBox.Focus();
            }
        }

        private void nameTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                usernameTxtBoxReg.Focus();
            }
        }

        private void usernameTxtBoxReg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                passwordTextBoxReg.Focus();
            }
        }

        private void passwordTextBoxReg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RegisterButton_Click(sender, e);
            }
        }

        private void passwordTextVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            passwordBox.Password = passwordTextVisible.Text;
        }

        private void passwordTextVisible_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void passwordTextVisibleReg_TextChanged(object sender, TextChangedEventArgs e)
        {
            passwordTextBoxReg.Password = passwordTextVisibleReg.Text;
        }

        private void remembermeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberUsername = true;
        }

        private void remembermeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberUsername = false;
        }
    }
}