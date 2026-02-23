using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Syndiceo.Windows
{
    public partial class UpdateWindow : Window
    {
        private const string VersionJsonUrl = "https://github.com/nnaskow/Syndiceo-Releases/releases/download/releases/version.json";
        private string latestSetupUrl = string.Empty;

        public UpdateWindow()
        {
            InitializeComponent();
        }
        private void AnimateProgress(double value)
        {
            double scale = value / 100.0; 


            var animation = new DoubleAnimation
            {
                To = scale,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } 
            };

            ProgressScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);


            ProgressText.Text = $"{(int)value}%";
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdateAsync();
            CurrentVersionTextBlock.Text = "Текуща версия: " + Properties.Settings.Default.appVersion;
        }

        private async void CheckAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            AnimateProgress(10);

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SyndiceoApp");

                AnimateProgress(30); // зареждане JSON

                string json = await client.GetStringAsync(VersionJsonUrl);

                AnimateProgress(60); // парсване

                var latest = JsonSerializer.Deserialize<VersionInfo>(json);

                string currentVersion = Properties.Settings.Default.appVersion;
                Version latestVersion = new Version(latest.latestVersion);
                latestSetupUrl = latest.url;

                AnimateProgress(90); // сравняване на версии

                if (latestVersion > new Version(currentVersion))
                {
                    AnimateProgress(100); // завършване
                    MessageBox.Show($"Налична е нова версия {latestVersion}. Натиснете 'Актуализирай'.",
                                    "Актуализация", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateButton.IsEnabled = true;
                    UpdateButton.Opacity = 1;
                }
                else
                {
                    AnimateProgress(100);
                    MessageBox.Show("Вие използвате най-новата версия.", "Актуализация", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateButton.IsEnabled = false;
                    UpdateButton.Opacity = 0.475;
                }
            }
            catch
            {
                AnimateProgress(100);
                UpdateButton.IsEnabled = false;
                UpdateButton.Opacity = 0.475;
            }
        }


        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(latestSetupUrl))
                return;

            try
            {
                AutoCloseMessageBox.ShowUntilExit("Изтегляне и стартиране на обновлението...");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SyndiceoApp");

                string tempPath = Path.Combine(Path.GetTempPath(), "SyndiceoSetup.exe");

                using (var response = await client.GetAsync(latestSetupUrl))
                {
                    response.EnsureSuccessStatusCode();

                    await using (var fs = File.Create(tempPath))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка при актуализация: " + ex.Message,
                                "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public class VersionInfo
        {
            public string latestVersion { get; set; }
            public string url { get; set; }
        }
    }
}
