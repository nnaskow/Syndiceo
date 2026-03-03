using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
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
using System.IO;
using Syndiceo.Models;
using Syndiceo.Data.Models;
using Syndiceo.Data;
namespace Syndiceo.Windows
{
    /// <summary>
    /// Interaction logic for ChangeUserDataWindow.xaml
    /// </summary>
    public partial class ChangeUserDataWindow : Window
    {
        public ChangeUserDataWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ChangeName == true)
            {
                ChangeNameGrid.Visibility = Visibility.Visible;
            }
            else if (Properties.Settings.Default.ChangePassword == true)
            {
                ChangePasswordGrid.Visibility = Visibility.Visible;
            }
            else if (Properties.Settings.Default.ChangeUsername == true)
            {
                ChangeUsernameGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ChangeEmailGrid.Visibility = Visibility.Visible;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ChangeUsername = false;
            Properties.Settings.Default.ChangePassword = false;
            Properties.Settings.Default.ChangeEmail = false;
            Properties.Settings.Default.ChangeName = false;
        }

        private void changeNameButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault(u => u.RememberMe);
                if (user == null)
                {
                    MessageBox.Show("Не е намерен активен потребител.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.Password != ConfirmPasswordForName.Password)
                {
                    MessageBox.Show("Невалидна парола!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewNameTextBox.Text))
                {
                    MessageBox.Show("Моля въведете ново име.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.PersonName = NewNameTextBox.Text.Trim();
                Properties.Settings.Default.isNameChanged = true;
                Properties.Settings.Default.Save();
                context.SaveChanges();

                MessageBox.Show("Името беше сменено успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        // Смяна на потребителско име
        private void changeUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault(u => u.RememberMe);
                if (user == null)
                {
                    MessageBox.Show("Не е намерен активен потребител.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.Password != ConfirmPasswordForUsername.Password)
                {
                    MessageBox.Show("Невалидна парола!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newUsername = NewUsernameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newUsername))
                {
                    MessageBox.Show("Моля въведете ново потребителско име.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (context.Logins.Any(u => u.Username == newUsername))
                {
                    MessageBox.Show("Това потребителско име вече съществува!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.Username = newUsername;
                context.SaveChanges();

                MessageBox.Show("Потребителското име беше сменено успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Properties.Settings.Default.RememberPassword = false;
                Properties.Settings.Default.RememberUsername = false;
                Properties.Settings.Default.isUsernameChanged = true;
                Properties.Settings.Default.Save();

                this.Close();
            }
        }


        // Смяна на имейл
        private void changeEmailButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault(u => u.RememberMe);
                if (user == null)
                {
                    MessageBox.Show("Не е намерен активен потребител.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.Password != ConfirmPasswordForEmail.Password)
                {
                    MessageBox.Show("Невалидна парола!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewEmailTextBox.Text))
                {
                    MessageBox.Show("Моля въведете нов имейл.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.Email = NewEmailTextBox.Text.Trim();
                context.SaveChanges();

                MessageBox.Show("Имейлът беше сменен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        // Смяна на парола
        private void changePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault(u => u.RememberMe);
                if (user == null)
                {
                    MessageBox.Show("Не е намерен активен потребител.", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (user.Password != OldPasswordBox.Password)
                {
                    MessageBox.Show("Старата парола е грешна!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    MessageBox.Show("Моля въведете нова парола.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (NewPasswordBox.Password != ConfirmNewPasswordBox.Password)
                {
                    MessageBox.Show("Новите пароли не съвпадат!", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.Password = NewPasswordBox.Password;
                context.SaveChanges();

                string content = $"Вашите данни за вход във вашето приложение:\nПотребителско име: {user.Username}\nПарола: {user.Password}";

                string appDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Syndiceo");

                if (!Directory.Exists(appDataFolder))
                    Directory.CreateDirectory(appDataFolder);

                string fileName = "login.txt";
                string fullPath = System.IO.Path.Combine(appDataFolder, fileName);

                File.WriteAllText(fullPath, content);
                MessageBox.Show("Паролата беше сменена успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Properties.Settings.Default.RememberPassword = false;
                Properties.Settings.Default.isPasswordChanged = true;
                Properties.Settings.Default.Save();
                this.Close();
            }
        }
    }
}

