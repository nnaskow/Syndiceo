using System.Threading.Tasks;
using System.Windows;

namespace Syndiceo.Windows
{
    public partial class AutoCloseMessageBox : Window
    {
        public AutoCloseMessageBox(string message, int delayMs = 2000)
        {
            InitializeComponent();
            MessageText.Text = message;

            Loaded += async (s, e) =>
            {
                await Task.Delay(delayMs);
                this.Close();
            };
        }

        public static void Show(string message, int delayMs = 2000)
        {
            var msgBox = new AutoCloseMessageBox(message, delayMs);
            msgBox.ShowDialog();
        }
        public static void ShowUntilExit(string message)
        {
            var msgBox = new AutoCloseMessageBox(message)
            {
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            msgBox.Show();

            Application.Current.Exit += (s, e) =>
            {
                if (msgBox.IsVisible)
                    msgBox.Close();
            };
        }
    }
}
