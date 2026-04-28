using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Syndiceo.Windows;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using Syndiceo.Windows;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Syndiceo.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (Properties.Settings.Default.isUpdated == true)
            {
                Properties.Settings.Default.WhatsNewNeeded = true;
                Properties.Settings.Default.RememberPassword = false;
                Properties.Settings.Default.SavedPassword = "";
                Properties.Settings.Default.isUpdated = false;
                Properties.Settings.Default.Save();
            }

            Properties.Settings.Default.appVersion = "1.0.0";
            Properties.Settings.Default.Save();

            using (var context = new SyndiceoDBContext())
            {
                var firstUser = context.Logins.FirstOrDefault();

                if (firstUser == null)
                {
                    LoginGrid.Visibility = Visibility.Collapsed;
                    RegisterGrid.Visibility = Visibility.Visible;
                    SecuritySetupGrid.Visibility = Visibility.Collapsed;
                    nameTxtBox.Focus();
                    return;
                }

                if (!PasswordHasher.IsHashed(firstUser.Password))
                {
                    string oldPlainPassword = firstUser.Password;
                    firstUser.Password = PasswordHasher.HashPassword(oldPlainPassword);

                    if (!string.IsNullOrEmpty(firstUser.SecurityAnswerHash) && !PasswordHasher.IsHashed(firstUser.SecurityAnswerHash))
                    {
                        firstUser.SecurityAnswerHash = PasswordHasher.HashPassword(firstUser.SecurityAnswerHash.ToLower().Trim());
                    }

                    context.SaveChanges();

                    Properties.Settings.Default.RememberPassword = false;
                    Properties.Settings.Default.Save();
                }

                if (string.IsNullOrEmpty(firstUser.SecurityAnswerHash))
                {
                    LoginGrid.Visibility = Visibility.Collapsed;
                    RegisterGrid.Visibility = Visibility.Collapsed;
                    SecuritySetupGrid.Visibility = Visibility.Visible;
                    SetupAnswerTxtBox.Focus();
                    return;
                }

                SecuritySetupGrid.Visibility = Visibility.Collapsed;
                RegisterGrid.Visibility = Visibility.Collapsed;
                LoginGrid.Visibility = Visibility.Visible;

                var rememberedUser = context.Logins.FirstOrDefault(u => u.RememberMe);
                if (rememberedUser != null)
                {
                    remembermeCheckbox.IsChecked = rememberedUser.RememberMe;

                    bool rememberUsername = Properties.Settings.Default.RememberUsername;
                    bool rememberPassword = Properties.Settings.Default.RememberPassword;

                    if (rememberUsername)
                        usernameTxtBox.Text = rememberedUser.Username;

                    if (rememberPassword)
                    {
                        string encryptedPass = Properties.Settings.Default.SavedPassword;
                        passwordBox.Password = LocalEncryption.Unprotect(encryptedPass);
                    }

                    if (rememberUsername && string.IsNullOrEmpty(passwordBox.Password))
                        passwordBox.Focus();
                    else if (!rememberUsername)
                        usernameTxtBox.Focus();
                    else
                        Keyboard.ClearFocus();
                }
                else
                {
                    remembermeCheckbox.IsChecked = false;
                    usernameTxtBox.Focus();
                }
            }

            Properties.Settings.Default.Save();
        }
        private void DatabaseMigration()
        {
            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    context.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при синхронизация: " + ex.Message);
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTxtBoxReg.Text;
            string password = passwordTextBoxReg.Password;
            string personName = nameTxtBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(personName))
            {
                MessageBox.Show("Моля, попълнете всички полета!");
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
                        Password = PasswordHasher.HashPassword(password),
                        PersonName = personName
                    };

                    context.Logins.Add(login);
                    context.SaveChanges();
                }

                MessageBox.Show("Успешна регистрация! Сега настройте защитния си въпрос за сигурност.");

                RegisterGrid.Visibility = Visibility.Collapsed;
                SecuritySetupGrid.Visibility = Visibility.Visible;
                SetupAnswerTxtBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при регистрация: " + ex.Message);
            }
        }

        private void SaveSecurityButton_Click(object sender, RoutedEventArgs e)
        {
            string answer = SetupAnswerTxtBox.Text.Trim();

            if (string.IsNullOrEmpty(answer))
            {
                MessageBox.Show("Моля, въведете отговор на въпроса!");
                return;
            }

            try
            {
                using (var context = new SyndiceoDBContext())
                {
                    var user = context.Logins.OrderByDescending(u => u.Id).FirstOrDefault();

                    if (user != null)
                    {
                        user.SecurityQuestion = (SetupQuestionCombo.SelectedItem as ComboBoxItem).Content.ToString();
                        user.SecurityAnswerHash = PasswordHasher.HashPassword(answer.ToLower());

                        context.SaveChanges();

                        MessageBox.Show("Настройките за сигурност са запазени успешно!");

                        SecuritySetupGrid.Visibility = Visibility.Collapsed;
                        LoginGrid.Visibility = Visibility.Visible;
                        usernameTxtBox.Text = user.Username;
                        passwordBox.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при запис на сигурността: " + ex.Message);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTxtBox.Text;
            string password = passwordBox.Password;

            using (var context = new SyndiceoDBContext())
            {
                var user = context.Logins.FirstOrDefault(u => u.Username == username);

                if (user != null && PasswordHasher.VerifyPassword(password, user.Password))
                {
                    foreach (var u in context.Logins) u.RememberMe = false;

                    if (remembermeCheckbox.IsChecked == true)
                    {
                        user.RememberMe = true;
                        Properties.Settings.Default.SavedPassword = LocalEncryption.Protect(passwordBox.Password);
                        Properties.Settings.Default.RememberPassword = true;
                    }
                    else
                    {
                        user.RememberMe = false;
                        Properties.Settings.Default.SavedPassword = "";
                        Properties.Settings.Default.RememberPassword = false;
                    }
                    context.SaveChanges();

                    ManagementWindow management = new ManagementWindow();
                    management.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Грешно потребителско име или парола!");
                }
            }
        }

        private void passwordTextVisible_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void passwordTextVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (passwordBox.Password != passwordTextVisible.Text)
            {
                passwordBox.Password = passwordTextVisible.Text;
            }
        }

        public void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            passwordTextVisible.Text = passwordBox.Password;
            passwordTextVisible.Visibility = Visibility.Visible;
            passwordBox.Visibility = Visibility.Collapsed;
            passwordTextVisible.Focus();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordBox.Password = passwordTextVisible.Text;
            passwordTextVisible.Visibility = Visibility.Collapsed;
            passwordBox.Visibility = Visibility.Visible;
            passwordBox.Focus();
        }
        public void ShowPasswordReg_Checked(object sender, RoutedEventArgs e) { passwordTextVisibleReg.Text = passwordTextBoxReg.Password; passwordTextVisibleReg.Visibility = Visibility.Visible; passwordTextBoxReg.Visibility = Visibility.Hidden; }
        public void ShowPasswordReg_Unchecked(object sender, RoutedEventArgs e) { passwordTextBoxReg.Password = passwordTextVisibleReg.Text; passwordTextVisibleReg.Visibility = Visibility.Hidden; passwordTextBoxReg.Visibility = Visibility.Visible; }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoginButton_Click(sender, e); }
        private void usernameTxtBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) passwordBox.Focus(); }
        private void nameTxtBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) usernameTxtBoxReg.Focus(); }
        private void usernameTxtBoxReg_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) passwordTextBoxReg.Focus(); }
        private void passwordTextBoxReg_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RegisterButton_Click(sender, e); }
        private void passwordTextVisibleReg_TextChanged(object sender, TextChangedEventArgs e) { if (passwordTextBoxReg.Password != passwordTextVisibleReg.Text) passwordTextBoxReg.Password = passwordTextVisibleReg.Text; }

        private void remembermeCheckbox_Checked(object sender, RoutedEventArgs e) => Properties.Settings.Default.RememberUsername = true;
        private void remembermeCheckbox_Unchecked(object sender, RoutedEventArgs e) => Properties.Settings.Default.RememberUsername = false;

        private void ForgottenPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            LoginGrid.Visibility = Visibility.Collapsed;
            ForgotPasswordGrid.Visibility = Visibility.Visible;
            using (var db = new SyndiceoDBContext())
            {
                var user = db.Logins.FirstOrDefault(u => u.Username == usernameTxtBox.Text);
                if (user != null && !string.IsNullOrEmpty(user.SecurityQuestion))
                {
                    SecurityQuestionTxt.Text = user.SecurityQuestion;
                }
                else
                {
                    MessageBox.Show("Потребителят не съществува или няма зададен защитен въпрос.");
                    BackToLoginBtn_Click(sender, e);
                }
            }
        }

        private void BackToLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            ForgotPasswordGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
        }

        private void RestorePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new SyndiceoDBContext())
            {
                var user = db.Logins.FirstOrDefault(u => u.Username == usernameTxtBox.Text);
                if (user != null)
                {
                    string answer = SecurityAnswerTxtBox.Text.Trim().ToLower();
                    if (PasswordHasher.VerifyPassword(answer, user.SecurityAnswerHash))
                    {
                        ResetPasswordGrid.Visibility = Visibility.Visible;
                        ForgotPasswordGrid.Visibility = Visibility.Collapsed;
                        SetupAnswerTxtBox.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Грешен отговор на защитния въпрос.");
                    }
                }
                else
                {
                    MessageBox.Show("Потребителят не съществува.");
                    BackToLoginBtn_Click(sender, e);
                }
            }
        }

        private void SaveNewPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new SyndiceoDBContext())
            {
                var user = db.Logins.FirstOrDefault(u => u.Username == usernameTxtBox.Text);
                if (user != null)
                {
                    string newPassword = NewPasswordBox.Password;
                    if (string.IsNullOrWhiteSpace(newPassword))
                    {
                        MessageBox.Show("Моля, въведете нова парола.");
                        return;
                    }
                    user.Password = PasswordHasher.HashPassword(newPassword);
                    db.SaveChanges();
                    MessageBox.Show("Паролата е успешно променена. Моля, влезте отново.");
                    ResetPasswordGrid.Visibility = Visibility.Collapsed;
                    LoginGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Потребителят не съществува.");
                    BackToLoginBtn_Click(sender, e);
                }
            }
        }
    }
}