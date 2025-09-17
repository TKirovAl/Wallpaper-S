using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Wpf;

namespace LiveWallpaperApp
{
    public partial class MainWindow : Window
    {
        private MediaPlayer wallpaperPlayer;
        private MediaElement previewVideo;
        private Image previewImage;
        private WebView2 previewWeb;
        private string currentMediaPath;
        private bool isWallpaperActive = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupPreviewControls();
            SetupUI();
            wallpaperPlayer = new MediaPlayer();
        }

        private void SetupUI()
        {
            // Установка текстов на русском языке
            HeaderTitle.Text = "Live Wallpaper Manager";
            HeaderSubtitle.Text = "Импортируйте медиа файлы для создания живых обоев";

            ControlsTitle.Text = "Управление";
            SelectFileButton.Content = "Выбрать файл";
            StreamUrlLabel.Text = "Или введите URL стрима:";
            LoadStreamButton.Content = "Загрузить стрим";
            SetWallpaperButton.Content = "Установить обои";
            StopWallpaperButton.Content = "Остановить обои";

            StatusLabel.Text = "Статус:";
            StatusText.Text = "Готов к работе";
            CurrentFileLabel.Text = "Текущий файл:";
            CurrentFileText.Text = "Не выбран";

            PreviewTitle.Text = "Предварительный просмотр";
            PreviewDefaultText.Text = "Выберите медиа файл для предварительного просмотра";

            FooterText.Text = "Поддерживаемые форматы: JPG, PNG, GIF, MP4, AVI, MKV, WMV, MOV, FLV, WebM, HTTP/RTMP стримы";
        }

        private void SetupPreviewControls()
        {
            // Очищаем превью грид, но оставляем PreviewDefaultText
            var defaultText = PreviewDefaultText;
            PreviewGrid.Children.Clear();

            // Создаем элементы управления превью
            previewVideo = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Close,
                Stretch = Stretch.Uniform,
                Visibility = Visibility.Collapsed
            };
            previewVideo.MediaEnded += (s, e) => previewVideo.Position = TimeSpan.Zero;

            previewImage = new Image
            {
                Stretch = Stretch.Uniform,
                Visibility = Visibility.Collapsed
            };

            previewWeb = new WebView2
            {
                Visibility = Visibility.Collapsed
            };

            PreviewGrid.Children.Add(previewVideo);
            PreviewGrid.Children.Add(previewImage);
            PreviewGrid.Children.Add(previewWeb);
            PreviewGrid.Children.Add(defaultText);
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите медиа файл",
                Filter = MediaHelper.GetOpenFileDialogFilter(),
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (!MediaHelper.IsValidMediaFile(openFileDialog.FileName))
                {
                    MessageBox.Show("Выбран неподдерживаемый тип файла", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                currentMediaPath = openFileDialog.FileName;
                LoadPreview(currentMediaPath);
                UpdateUI();
            }
        }

        private async void LoadStream_Click(object sender, RoutedEventArgs e)
        {
            string streamUrl = StreamUrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(streamUrl))
            {
                MessageBox.Show("Введите URL стрима", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            streamUrl = MediaHelper.NormalizeStreamUrl(streamUrl);

            if (!MediaHelper.ValidateStreamUrl(streamUrl))
            {
                MessageBox.Show("Некорректный URL стрима", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            currentMediaPath = streamUrl;
            await LoadStreamPreview(streamUrl);
            UpdateUI();
        }

        private void LoadPreview(string mediaPath)
        {
            try
            {
                HideAllPreviews();

                var mediaType = MediaHelper.GetMediaType(mediaPath);

                switch (mediaType)
                {
                    case MediaHelper.MediaType.StaticImage:
                        LoadImagePreview(mediaPath);
                        break;

                    case MediaHelper.MediaType.AnimatedImage:
                        LoadImagePreview(mediaPath); // GIF будет анимирован автоматически
                        break;

                    case MediaHelper.MediaType.Video:
                        LoadVideoPreview(mediaPath);
                        break;

                    case MediaHelper.MediaType.Stream:
                        LoadStreamPreview(mediaPath).ConfigureAwait(false);
                        break;

                    default:
                        StatusText.Text = "Неподдерживаемый формат файла";
                        return;
                }

                StatusText.Text = "Файл загружен успешно";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadImagePreview(string imagePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.DecodePixelWidth = 400;
            bitmap.EndInit();

            previewImage.Source = bitmap;
            previewImage.Visibility = Visibility.Visible;
        }

        private void LoadVideoPreview(string videoPath)
        {
            previewVideo.Source = new Uri(videoPath, UriKind.Absolute);
            previewVideo.Visibility = Visibility.Visible;
            previewVideo.Play();
        }

        private async System.Threading.Tasks.Task LoadStreamPreview(string streamUrl)
        {
            try
            {
                HideAllPreviews();

                await previewWeb.EnsureCoreWebView2Async();

                string html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ margin: 0; padding: 0; background: black; overflow: hidden; }}
                            video {{ max-width: 100%; max-height: 100%; width: auto; height: auto; }}
                            .container {{ display: flex; justify-content: center; align-items: center; height: 100vh; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <video autoplay muted controls>
                                <source src='{streamUrl}' type='video/mp4'>
                                <p>Стрим недоступен</p>
                            </video>
                        </div>
                    </body>
                    </html>";

                previewWeb.NavigateToString(html);
                previewWeb.Visibility = Visibility.Visible;
                StatusText.Text = "Стрим загружен";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка загрузки стрима: {ex.Message}";
            }
        }

        private void HideAllPreviews()
        {
            previewVideo.Visibility = Visibility.Collapsed;
            previewImage.Visibility = Visibility.Collapsed;
            previewWeb.Visibility = Visibility.Collapsed;

            previewVideo.Stop();

            // Скрываем текст по умолчанию
            PreviewDefaultText.Visibility = Visibility.Collapsed;
        }

        private void SetWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentMediaPath))
            {
                MessageBox.Show("Сначала выберите медиа файл", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Если это статическое изображение, используем стандартный API Windows
                if (MediaHelper.IsStaticImage(currentMediaPath))
                {
                    if (WallpaperAPI.SetStaticWallpaper(currentMediaPath))
                    {
                        StatusText.Text = "Статические обои установлены";
                        isWallpaperActive = true;
                        UpdateUI();
                    }
                    else
                    {
                        StatusText.Text = "Ошибка установки статических обоев";
                    }
                }
                else if (MediaHelper.IsAnimatedMedia(currentMediaPath))
                {
                    // Для видео, GIF и стримов используем живые обои
                    wallpaperPlayer.LoadMedia(currentMediaPath);
                    wallpaperPlayer.StartAsWallpaper();
                    StatusText.Text = "Живые обои активированы";
                    isWallpaperActive = true;
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Неподдерживаемый тип медиа файла", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка установки обоев: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopWallpaper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wallpaperPlayer.StopWallpaper();

                // Восстанавливаем стандартные обои Windows
                WallpaperAPI.SetStaticWallpaper("");

                StatusText.Text = "Обои остановлены";
                isWallpaperActive = false;
                UpdateUI();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void UpdateUI()
        {
            bool hasMedia = !string.IsNullOrEmpty(currentMediaPath);

            SetWallpaperButton.IsEnabled = hasMedia && !isWallpaperActive;
            StopWallpaperButton.IsEnabled = isWallpaperActive;

            if (hasMedia)
            {
                CurrentFileText.Text = $"{Path.GetFileName(currentMediaPath)}\n{MediaHelper.GetFileInfo(currentMediaPath)}";
            }
            else
            {
                CurrentFileText.Text = "Не выбран";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            wallpaperPlayer?.StopWallpaper();
            wallpaperPlayer?.Close();
            base.OnClosed(e);
        }
    }
}