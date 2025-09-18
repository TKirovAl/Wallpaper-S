using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace LiveWallpaperApp
{
    public partial class MainWindow : Window
    {
        private WallpaperMediaPlayer? currentMediaPlayer;
        private string? currentFilePath;
        private bool isWallpaperActive = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Заголовки
            HeaderTitle.Text = "Live Wallpaper Manager";
            HeaderSubtitle.Text = "Создавайте живые обои из видео, GIF и изображений";

            // Элементы управления
            ControlsTitle.Text = "Управление";
            SelectFileButton.Content = "📁 Выбрать файл";
            StreamUrlLabel.Text = "URL стрима:";
            LoadStreamButton.Content = "📺 Загрузить стрим";
            SetWallpaperButton.Content = "🖼️ Установить обои";
            StopWallpaperButton.Content = "🗑️ Остановить обои";

            // Превью
            PreviewTitle.Text = "Предварительный просмотр";
            PreviewDefaultText.Text = "Выберите файл для просмотра";

            // Статус
            StatusLabel.Text = "Статус:";
            StatusText.Text = "Готов к работе";
            CurrentFileLabel.Text = "Текущий файл:";
            CurrentFileText.Text = "Не выбран";

            // Футер
            FooterText.Text = "Live Wallpaper Manager v1.0 - Поддерживает JPG, PNG, MP4, GIF, стримы";
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл для обоев",
                Filter = "Все поддерживаемые|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.avi;*.mov;*.wmv;*.mkv;*.webm|" +
                        "Изображения|*.jpg;*.jpeg;*.png;*.bmp|" +
                        "Видео|*.mp4;*.avi;*.mov;*.wmv;*.mkv;*.webm|" +
                        "GIF|*.gif|" +
                        "Все файлы|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                currentFilePath = openFileDialog.FileName;
                LoadMediaFile(currentFilePath);
            }
        }

        private void LoadStream_Click(object sender, RoutedEventArgs e)
        {
            string streamUrl = StreamUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(streamUrl))
            {
                UpdateStatus("Введите URL стрима", false);
                return;
            }

            if (!Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
            {
                UpdateStatus("Некорректный URL стрима", false);
                return;
            }

            currentFilePath = streamUrl;
            LoadMediaStream(streamUrl);
        }

        private void LoadMediaFile(string filePath)
        {
            try
            {
                UpdateStatus("Загрузка файла...", true);
                CurrentFileText.Text = Path.GetFileName(filePath);

                string extension = Path.GetExtension(filePath).ToLower();

                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                        LoadImagePreview(filePath);
                        break;
                    case ".gif":
                        LoadGifPreview(filePath);
                        break;
                    case ".mp4":
                    case ".avi":
                    case ".mov":
                    case ".wmv":
                    case ".mkv":
                    case ".webm":
                        LoadVideoPreview(filePath);
                        break;
                    default:
                        UpdateStatus("Неподдерживаемый формат файла", false);
                        return;
                }

                SetWallpaperButton.IsEnabled = true;
                UpdateStatus("Файл загружен успешно", true);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки: {ex.Message}", false);
            }
        }

        private void LoadImagePreview(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 400; // Ограничиваем размер для превью
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform
                };

                PreviewGrid.Children.Clear();
                PreviewGrid.Children.Add(image);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки изображения: {ex.Message}", false);
            }
        }

        private void LoadGifPreview(string gifPath)
        {
            try
            {
                var image = new Image();
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(gifPath);
                bitmap.EndInit();

                image.Source = bitmap;
                image.Stretch = Stretch.Uniform;

                PreviewGrid.Children.Clear();
                PreviewGrid.Children.Add(image);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки GIF: {ex.Message}", false);
            }
        }

        private void LoadVideoPreview(string videoPath)
        {
            try
            {
                var mediaElement = new MediaElement
                {
                    Source = new Uri(videoPath),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Close,
                    Stretch = Stretch.Uniform,
                    Volume = 0 // Без звука в превью
                };

                mediaElement.MediaOpened += (s, e) => mediaElement.Play();
                mediaElement.MediaEnded += (s, e) =>
                {
                    mediaElement.Position = TimeSpan.Zero;
                    mediaElement.Play();
                };

                PreviewGrid.Children.Clear();
                PreviewGrid.Children.Add(mediaElement);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки видео: {ex.Message}", false);
            }
        }

        private void LoadMediaStream(string streamUrl)
        {
            try
            {
                UpdateStatus("Подключение к стриму...", true);
                CurrentFileText.Text = streamUrl;

                var mediaElement = new MediaElement
                {
                    Source = new Uri(streamUrl),
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Close,
                    Stretch = Stretch.Uniform,
                    Volume = 0
                };

                mediaElement.MediaOpened += (s, e) =>
                {
                    mediaElement.Play();
                    SetWallpaperButton.IsEnabled = true;
                    UpdateStatus("Стрим подключен", true);
                };

                mediaElement.MediaFailed += (s, e) =>
                {
                    UpdateStatus($"Ошибка стрима: {e.ErrorException?.Message}", false);
                };

                PreviewGrid.Children.Clear();
                PreviewGrid.Children.Add(mediaElement);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка подключения к стриму: {ex.Message}", false);
            }
        }

        private void SetWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                UpdateStatus("Файл не выбран", false);
                return;
            }

            try
            {
                UpdateStatus("Установка обоев...", true);

                string extension = Path.GetExtension(currentFilePath).ToLower();

                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                {
                    // Статичные обои
                    if (WallpaperAPI.SetStaticWallpaper(currentFilePath))
                    {
                        UpdateStatus("Статичные обои установлены", true);
                        isWallpaperActive = true;
                        StopWallpaperButton.IsEnabled = true;
                    }
                    else
                    {
                        UpdateStatus("Ошибка установки статичных обоев", false);
                    }
                }
                else
                {
                    // Живые обои (видео/GIF/стрим)
                    StopCurrentWallpaper();

                    currentMediaPlayer = new WallpaperMediaPlayer(currentFilePath);
                    if (currentMediaPlayer.Start())
                    {
                        UpdateStatus("Живые обои установлены", true);
                        isWallpaperActive = true;
                        StopWallpaperButton.IsEnabled = true;
                    }
                    else
                    {
                        UpdateStatus("Ошибка установки живых обоев", false);
                        currentMediaPlayer?.Dispose();
                        currentMediaPlayer = null;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка установки обоев: {ex.Message}", false);
            }
        }

        private void StopWallpaper_Click(object sender, RoutedEventArgs e)
        {
            StopCurrentWallpaper();
            UpdateStatus("Обои остановлены", true);
        }

        private void StopCurrentWallpaper()
        {
            try
            {
                currentMediaPlayer?.Stop();
                currentMediaPlayer?.Dispose();
                currentMediaPlayer = null;

                isWallpaperActive = false;
                StopWallpaperButton.IsEnabled = false;

                // Возвращаем стандартные обои Windows
                WallpaperAPI.RestoreDefaultWallpaper();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка остановки обоев: {ex.Message}", false);
            }
        }

        private void UpdateStatus(string message, bool isSuccess)
        {
            StatusText.Text = message;
            StatusText.Foreground = isSuccess ?
                new SolidColorBrush(Color.FromRgb(0x38, 0xA1, 0x69)) :
                new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCurrentWallpaper();
            base.OnClosed(e);
        }
    }
}