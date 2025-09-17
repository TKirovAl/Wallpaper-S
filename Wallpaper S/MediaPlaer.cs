using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

public class MediaPlayer : Window
{
    private MediaElement videoPlayer;
    private Image imageViewer;
    private WebView2 webViewer;
    private Grid contentGrid;
    private string currentMediaPath;
    private MediaType currentMediaType;

    public enum MediaType
    {
        Image,
        Video,
        Gif,
        Stream
    }

    public MediaPlayer()
    {
        InitializeComponents();
        SetupWindow();
    }

    private void InitializeComponents()
    {
        contentGrid = new Grid();
        Content = contentGrid;

        // Video player для видео файлов
        videoPlayer = new MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Close,
            Stretch = Stretch.UniformToFill,
            Visibility = Visibility.Collapsed
        };
        videoPlayer.MediaEnded += (s, e) => videoPlayer.Position = TimeSpan.Zero;

        // Image viewer для статических изображений и GIF
        imageViewer = new Image
        {
            Stretch = Stretch.UniformToFill,
            Visibility = Visibility.Collapsed
        };

        // WebView2 для стримов
        webViewer = new WebView2
        {
            Visibility = Visibility.Collapsed
        };

        contentGrid.Children.Add(videoPlayer);
        contentGrid.Children.Add(imageViewer);
        contentGrid.Children.Add(webViewer);
    }

    private void SetupWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = false;
        ShowInTaskbar = false;

        // Получаем размеры экрана
        var bounds = SystemParameters.PrimaryScreenWidth;
        var height = SystemParameters.PrimaryScreenHeight;

        Width = bounds;
        Height = height;
        Left = 0;
        Top = 0;

        Background = Brushes.Black;
    }

    public async void LoadMedia(string mediaPath)
    {
        currentMediaPath = mediaPath;
        currentMediaType = DetermineMediaType(mediaPath);

        HideAllPlayers();

        switch (currentMediaType)
        {
            case MediaType.Image:
                LoadImage(mediaPath);
                break;
            case MediaType.Video:
                LoadVideo(mediaPath);
                break;
            case MediaType.Gif:
                LoadGif(mediaPath);
                break;
            case MediaType.Stream:
                await LoadStream(mediaPath);
                break;
        }
    }

    private MediaType DetermineMediaType(string path)
    {
        if (path.StartsWith("http") || path.StartsWith("rtmp") || path.StartsWith("rtsp"))
            return MediaType.Stream;

        string extension = Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".mp4" or ".avi" or ".mkv" or ".wmv" or ".mov" or ".flv" or ".webm" => MediaType.Video,
            ".gif" => MediaType.Gif,
            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".tiff" or ".webp" => MediaType.Image,
            _ => MediaType.Image
        };
    }

    private void LoadImage(string imagePath)
    {
        try
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();

            imageViewer.Source = bitmap;
            imageViewer.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
        }
    }

    private void LoadVideo(string videoPath)
    {
        try
        {
            videoPlayer.Source = new Uri(videoPath, UriKind.Absolute);
            videoPlayer.Visibility = Visibility.Visible;
            videoPlayer.Play();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки видео: {ex.Message}");
        }
    }

    private void LoadGif(string gifPath)
    {
        try
        {
            var gif = new BitmapImage();
            gif.BeginInit();
            gif.UriSource = new Uri(gifPath, UriKind.Absolute);
            gif.EndInit();

            imageViewer.Source = gif;
            imageViewer.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки GIF: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task LoadStream(string streamUrl)
    {
        try
        {
            await webViewer.EnsureCoreWebView2Async();

            string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ margin: 0; padding: 0; background: black; overflow: hidden; }}
                        video {{ width: 100vw; height: 100vh; object-fit: cover; }}
                    </style>
                </head>
                <body>
                    <video autoplay muted loop>
                        <source src='{streamUrl}' type='video/mp4'>
                    </video>
                </body>
                </html>";

            webViewer.NavigateToString(html);
            webViewer.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки стрима: {ex.Message}");
        }
    }

    private void HideAllPlayers()
    {
        videoPlayer.Visibility = Visibility.Collapsed;
        imageViewer.Visibility = Visibility.Collapsed;
        webViewer.Visibility = Visibility.Collapsed;

        videoPlayer.Stop();
    }

    public void StartAsWallpaper()
    {
        Show();
        WallpaperAPI.SetParentToDesktop(new System.Windows.Interop.WindowInteropHelper(this).Handle);
    }

    public void StopWallpaper()
    {
        Hide();
        HideAllPlayers();
    }
}