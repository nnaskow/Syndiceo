using Syndiceo.Data.Models;
using Syndiceo.Utilities;
using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Windows;
using System.Windows.Media.Animation;
using Syndiceo.Data.Models;

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

            DatabaseMigration();

            Properties.Settings.Default.appVersion = "1.0.0";
            Properties.Settings.Default.Save();
            if (Properties.Settings.Default.WhatsNewNeeded == true)
            {
                WhatsNewWindow whatsNew = new WhatsNewWindow();
                whatsNew.ShowDialog();
            }

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
    /*    private void ApplyIdempotentUpdate()
        {
            using (var context = new SyndiceoDBContext())
            {
                string script = @"
SET XACT_ABORT ON;

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;


BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Addresses] (
        [AddressID] int NOT NULL IDENTITY,
        [Street] nvarchar(100) NOT NULL,
        CONSTRAINT [PK__Addresse__091C2A1B23DF249D] PRIMARY KEY ([AddressID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Kind] nvarchar(50) NULL,
        [Appliance] nvarchar(20) NULL,
        CONSTRAINT [PK__Categori__3214EC072BF5793F] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Logins] (
        [Id] int NOT NULL IDENTITY,
        [username] nvarchar(100) NOT NULL,
        [password] nvarchar(100) NOT NULL,
        [person_name] nvarchar(50) NOT NULL,
        [email] nvarchar(100) NULL,
        [RememberMe] bit NOT NULL,
        CONSTRAINT [PK_Logins] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Blocks] (
        [BlockID] int NOT NULL IDENTITY,
        [AddressID] int NOT NULL,
        [BlockName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK__Blocks__1442151126C24296] PRIMARY KEY ([BlockID]),
        CONSTRAINT [FK__Blocks__AddressI__3B75D760] FOREIGN KEY ([AddressID]) REFERENCES [Addresses] ([AddressID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Entrances] (
        [EntranceID] int NOT NULL IDENTITY,
        [BlockID] int NOT NULL,
        [EntranceName] nvarchar(50) NOT NULL,
        [MaintenanceHistory] nvarchar(max) NULL,
        CONSTRAINT [PK__Entrance__C52C8B3825D0F9DD] PRIMARY KEY ([EntranceID]),
        CONSTRAINT [FK__Entrances__Block__3E52440B] FOREIGN KEY ([BlockID]) REFERENCES [Blocks] ([BlockID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [TotalSum] (
        [Id] int NOT NULL IDENTITY,
        [EntranceId] int NULL,
        [Summary] int NOT NULL,
        CONSTRAINT [PK__TotalSum__3214EC070AB196E0] PRIMARY KEY ([Id]),
        CONSTRAINT [FK__TotalSum__Entran__756D6ECB] FOREIGN KEY ([EntranceId]) REFERENCES [Blocks] ([BlockID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Cashbox] (
        [Id] int NOT NULL IDENTITY,
        [CurrentBalance] decimal(12,2) NOT NULL,
        [EntranceId] int NOT NULL,
        CONSTRAINT [PK__Cashbox__3214EC077283E26E] PRIMARY KEY ([Id]),
        CONSTRAINT [FK__Cashbox__Entranc__4E53A1AA] FOREIGN KEY ([EntranceId]) REFERENCES [Entrances] ([EntranceID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Documents] (
        [DocumentId] int NOT NULL IDENTITY,
        [FileName] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [UploadDate] datetime NOT NULL,
        [EntranceId] int NOT NULL,
        CONSTRAINT [PK__Document__1ABEEF0FEFCCF606] PRIMARY KEY ([DocumentId]),
        CONSTRAINT [FK__Documents__Entra__18EBB532] FOREIGN KEY ([EntranceId]) REFERENCES [Entrances] ([EntranceID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [EntranceTransactions] (
        [Id] int NOT NULL IDENTITY,
        [EntranceId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [TransDate] date NOT NULL,
        [Description] nvarchar(255) NULL,
        [ApartmentId] int NULL,
        CONSTRAINT [PK__Entrance__3214EC0719957C67] PRIMARY KEY ([Id]),
        CONSTRAINT [FK__EntranceT__Categ__47A6A41B] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]),
        CONSTRAINT [FK__EntranceT__Entra__46B27FE2] FOREIGN KEY ([EntranceId]) REFERENCES [Entrances] ([EntranceID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Maintenance] (
        [MaintenanceId] int NOT NULL IDENTITY,
        [EntranceId] int NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Price] int NULL,
        [DateOfMaintenance] datetime NOT NULL,
        CONSTRAINT [PK__Maintena__E60542D591F22607] PRIMARY KEY ([MaintenanceId]),
        CONSTRAINT [FK_Maintenance_Entrance] FOREIGN KEY ([EntranceId]) REFERENCES [Entrances] ([EntranceID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Apartments] (
        [ApartmentID] int NOT NULL IDENTITY,
        [EntranceID] int NOT NULL,
        [ApartmentNumber] nvarchar(10) NOT NULL,
        [Note] nvarchar(max) NULL,
        [IsMarked] bit NOT NULL,
        [OwnerId] int NULL,
        [ResidentCount] int NULL,
        CONSTRAINT [PK__Apartmen__CBDF5744AB8A6A79] PRIMARY KEY ([ApartmentID]),
        CONSTRAINT [FK__Apartment__Entra__412EB0B6] FOREIGN KEY ([EntranceID]) REFERENCES [Entrances] ([EntranceID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [ApartmentTransactions] (
        [Id] int NOT NULL IDENTITY,
        [ApartmentId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [TransDate] date NOT NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK__Apartmen__3214EC070352F544] PRIMARY KEY ([Id]),
        CONSTRAINT [FK__Apartment__Apart__3F115E1A] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentID]),
        CONSTRAINT [FK__Apartment__Categ__40058253] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Debts] (
        [Id] int NOT NULL IDENTITY,
        [ApartmentId] int NULL,
        [TotalSum] decimal(10,2) NOT NULL,
        [PaidSum] decimal(10,2) NOT NULL,
        [RemainingSum] AS ([TotalSum]-[PaidSum]) PERSISTED,
        CONSTRAINT [PK__Debts__3214EC0780AC33ED] PRIMARY KEY ([Id]),
        CONSTRAINT [FK__Debts__Apartment__0E391C95] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE TABLE [Owners] (
        [OwnerId] int NOT NULL IDENTITY,
        [OwnerName] nvarchar(255) NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [ApartmentId] int NOT NULL,
        CONSTRAINT [PK__Owners__819385B8B15E0D62] PRIMARY KEY ([OwnerId]),
        CONSTRAINT [FK__Owners__Apartmen__5DCAEF64] FOREIGN KEY ([ApartmentId]) REFERENCES [Apartments] ([ApartmentID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Apartments_EntranceID] ON [Apartments] ([EntranceID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Apartments_OwnerId] ON [Apartments] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_ApartmentTransactions_ApartmentId] ON [ApartmentTransactions] ([ApartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_ApartmentTransactions_CategoryId] ON [ApartmentTransactions] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Blocks_AddressID] ON [Blocks] ([AddressID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Cashbox_EntranceId] ON [Cashbox] ([EntranceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Debts_ApartmentId] ON [Debts] ([ApartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Documents_EntranceId] ON [Documents] ([EntranceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Entrances_BlockID] ON [Entrances] ([BlockID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_EntranceTransactions_CategoryId] ON [EntranceTransactions] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_EntranceTransactions_EntranceId] ON [EntranceTransactions] ([EntranceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Logins_Username] ON [Logins] ([username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Maintenance_EntranceId] ON [Maintenance] ([EntranceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_Owners_ApartmentId] ON [Owners] ([ApartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    CREATE INDEX [IX_TotalSum_EntranceId] ON [TotalSum] ([EntranceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    ALTER TABLE [Apartments] ADD CONSTRAINT [FK_Apartments_Owners] FOREIGN KEY ([OwnerId]) REFERENCES [Owners] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111064111_InitialCreation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251111064111_InitialCreation', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320001536_ModelMigrationv103'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Apartments]') AND [c].[name] = N'ApartmentNumber');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Apartments] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Apartments] ALTER COLUMN [ApartmentNumber] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320001536_ModelMigrationv103'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260320001536_ModelMigrationv103', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320005103_HashMigration'
)
BEGIN
    ALTER TABLE [Logins] ADD [securityanswer_hash] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320005103_HashMigration'
)
BEGIN
    ALTER TABLE [Logins] ADD [securityquestion] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320005103_HashMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260320005103_HashMigration', N'9.0.10');
END;

COMMIT;


";

                try
                {
                    context.Database.ExecuteSqlRaw(script);
                }
                catch (Exception ex)
                {
                }
            }
        }*/
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
            string email = emailTxtBox.Text;

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
                        PersonName = personName,
                        Email = email
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
        private void usernameTxtBoxReg_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) emailTxtBox.Focus(); }
        private void emailTxtBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) passwordBox.Focus(); }
        private void passwordTextBoxReg_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RegisterButton_Click(sender, e); }
        private void passwordTextVisibleReg_TextChanged(object sender, TextChangedEventArgs e) { if (passwordTextBoxReg.Password != passwordTextVisibleReg.Text) passwordTextBoxReg.Password = passwordTextVisibleReg.Text; }

        private void remembermeCheckbox_Checked(object sender, RoutedEventArgs e) => Properties.Settings.Default.RememberUsername = true;
        private void remembermeCheckbox_Unchecked(object sender, RoutedEventArgs e) => Properties.Settings.Default.RememberUsername = false;

        private void ForgottenPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            LoginGrid.Visibility = Visibility.Collapsed;
            ForgotPasswordGrid.Visibility = Visibility.Visible;
            using(var db = new SyndiceoDBContext())
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