using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LiveWallpaperApp.Core;
using LiveWallpaperApp.Utils;

namespace LiveWallpaperApp.UI
{
    public partial class MainWindow : Window
    {
        private MediaProcessor _mediaProcessor;
        private WallpaperEngine _wallpaperEngine;
        private string _currentMediaPath;
        private MediaType _currentMediaType;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _mediaProcessor = new MediaProcessor();
            _wallpaperEngine = WallpaperEngine.Instance;
            
            // ��������� ����� ��������
            MediaPreview.MediaEnded += (s, e) =>
            {
                if (LoopPlayback.IsChecked == true)
                {
                    MediaPreview.Position = TimeSpan.Zero;
                    MediaPreview.Play();
                }
            };
        }

        private async void ImportImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "�����������|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|��� �����|*.*",
                Title = "�������� �����������"
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadMedia(dialog.FileName, MediaType.Image);
            }
        }

        private async void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "�����|*.mp4;*.avi;*.mov;*.wmv;*.mkv;*.webm|��� �����|*.*",
                Title = "�������� �����"
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadMedia(dialog.FileName, MediaType.Video);
            }
        }

        private async void ImportGif_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "GIF �����|*.gif|��� �����|*.*",
                Title = "�������� GIF"
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadMedia(dialog.FileName, MediaType.Gif);
            }
        }

        private void ImportStream_Click(object sender, RoutedEventArgs e)
        {
            var streamDialog = new StreamInputDialog();
            if (streamDialog.ShowDialog() == true)
            {
                var streamUrl = streamDialog.StreamUrl;
                if (!string.IsNullOrEmpty(streamUrl))
                {
                    LoadStreamMedia(streamUrl);
                }
            }
        }

        private async Task LoadMedia(string filePath, MediaType mediaType)
        {
            try
            {
                SetStatus("����������� �����...", true);
                
                _currentMediaPath = filePath;
                _currentMediaType = mediaType;

                HideAllPreviews();

                switch (mediaType)
                {
                    case MediaType.Image:
                        await LoadImagePreview(filePath);
                        break;
                    case MediaType.Video:
                    case MediaType.Gif:
                        await LoadVideoPreview(filePath);
                        break;
                }

                SetWallpaperBtn.IsEnabled = true;
                SetStatus($"����� ���������: {Path.GetFileName(filePath)}", false);
            }
            catch (Exception ex)
            {
                SetStatus($"������ ��������: {ex.Message}", false);
                MessageBox.Show($"������ �������� �����: {ex.Message}", "������", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadImagePreview(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImagePreview.Source = bitmap;
            ImagePreview.Visibility = Visibility.Visible;
        }

        private async Task LoadVideoPreview(string filePath)
        {
            // ����������� ��� �������������
            var processedPath = await _mediaProcessor.ProcessVideoForWallpaper(
                filePath, 
                GetQualitySettings(),
                MuteAudio.IsChecked == true);

            MediaPreview.Source = new Uri(processedPath);
            MediaPreview.Visibility = Visibility.Visible;
            MediaPreview.Play();
        }

        private void LoadStreamMedia(string streamUrl)
        {
            try
            {
                HideAllPreviews();
                
                _currentMediaPath = streamUrl;
                _currentMediaType = MediaType.Stream;

                // ��� ������� ���������� WebBrowser � HTML5 �������
                var html = GenerateStreamPlayerHtml(streamUrl);
                var tempFile = Path.Combine(Path.GetTempPath(), "stream_player.html");
                File.WriteAllText(tempFile, html);
                
                StreamPreview.Navigate(tempFile);
                StreamPreview.Visibility = Visibility.Visible;
                
                SetWallpaperBtn.IsEnabled = true;
                SetStatus($"����� ���������: {streamUrl}", false);
            }
            catch (Exception ex)
            {
                SetStatus($"������ ����������� � ������: {ex.Message}", false);
            }
        }

        private void HideAllPreviews()
        {
            PreviewPlaceholder.Visibility = Visibility.Visible;
            MediaPreview.Visibility = Visibility.Collapsed;
            ImagePreview.Visibility = Visibility.Collapsed;
            StreamPreview.Visibility = Visibility.Collapsed;
            
            MediaPreview.Stop();
        }

        private async void SetWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentMediaPath))
                return;

            try
            {
                SetStatus("��������� �����...", true);

                var settings = new WallpaperSettings
                {
                    FilePath = _currentMediaPath,
                    MediaType = _currentMediaType,
                    Loop = LoopPlayback.IsChecked == true,
                    Mute = MuteAudio.IsChecked == true,
                    Quality = GetQualitySettings()
                };

                await _wallpaperEngine.SetWallpaperAsync(settings);
                SetStatus("���� ������� �����������!", false);
            }
            catch (Exception ex)
            {
                SetStatus($"������ ��������� �����: {ex.Message}", false);
                MessageBox.Show($"������ ��������� �����: {ex.Message}", "������", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveWallpaper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _wallpaperEngine.RemoveWallpaper();
                SetStatus("����� ���� �������", false);
            }
            catch (Exception ex)
            {
                SetStatus($"������ �������� �����: {ex.Message}", false);
            }
        }

        private VideoQuality GetQualitySettings()
        {
            return QualityCombo.SelectedIndex switch
            {
                0 => VideoQuality.Low,
                1 => VideoQuality.Medium,
                2 => VideoQuality.High,
                _ => VideoQuality.Medium
            };
        }

        private string GenerateStreamPlayerHtml(string streamUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ margin: 0; padding: 0; background: black; }}
        video {{ width: 100%; height: 100vh; object-fit: cover; }}
    </style>
</head>
<body>
    <video autoplay muted loop>
        <source src=""{streamUrl}"" type=""video/mp4"">
        ��� ������� �� ������������ �����.
    </video>
</body>
</html>";
        }

        private void SetStatus(string message, bool showProgress)
        {
            StatusText.Text = message;
            ProgressBar.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            
            if (showProgress)
                ProgressBar.IsIndeterminate = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            MediaPreview?.Stop();
            _wallpaperEngine?.Cleanup();
            base.OnClosed(e);
        }
    }
}