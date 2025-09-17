using System;
using System.Windows;

namespace LiveWallpaperApp.UI
{
    public partial class StreamInputDialog : Window
    {
        public string StreamUrl { get; private set; }

        public StreamInputDialog()
        {
            InitializeComponent();
            StreamUrlTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var url = StreamUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Введите URL стрима", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidStreamUrl(url))
            {
                MessageBox.Show("Неверный формат URL", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StreamUrl = url;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool IsValidStreamUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "rtmp";
            }
            catch
            {
                return false;
            }
        }
    }
}