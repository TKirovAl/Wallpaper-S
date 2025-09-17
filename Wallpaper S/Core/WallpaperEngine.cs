using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveWallpaperApp.Utils;

namespace LiveWallpaperApp.Core
{
    public class WallpaperEngine
    {
        private static WallpaperEngine _instance;
        public static WallpaperEngine Instance => _instance ??= new WallpaperEngine();

        private MediaElement _wallpaperPlayer;
        private Window _wallpaperWindow;
        private Process _wallpaperProcess;
        private MediaProcessor _mediaProcessor;

        private WallpaperEngine()
        {
            _mediaProcessor = new MediaProcessor();
        }

        public async Task SetWallpaperAsync(WallpaperSettings settings)
        {
            try
            {
                // Останавливаем предыдущие обои
                RemoveWallpaper();

                switch (settings.MediaType)
                {
                    case MediaType.Image:
                        await SetStaticWallpaper(settings);
                        break;
                    case MediaType.Video:
                    case MediaType.Gif:
                        await SetVideoWallpaper(settings);
                        break;
                    case MediaType.Stream:
                        await SetStreamWallpaper(settings);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка установки обоев: {ex.Message}", ex);
            }
        }

        private async Task SetStaticWallpaper(WallpaperSettings settings)
        {
            var processedPath = _mediaProcessor.ProcessImageForWallpaper(
                settings.FilePath, settings.Quality);

            // Используем стандартный Windows API для статичных обоев
            if (!WinAPI.SystemParametersInfo(WinAPI.SPI_SETDESKWALLPAPER, 0,
                processedPath, WinAPI.SPIF_UPDATEINIFILE | WinAPI.SPIF_SENDCHANGE))
            {
                throw new Exception("Не удалось установить статичные обои");
            }
        }

        private async Task SetVideoWallpaper(WallpaperSettings settings)
        {
            // Обрабатываем видео для оптимального воспроизведения
            var processedPath = await _mediaProcessor.ProcessVideoForWallpaper(
                settings.FilePath, settings.Quality, settings.Mute);

            // Создаем окно для живых обоев
            Application.Current.Dispatcher.Invoke(() =>
            {
                CreateWallpaperWindow(processedPath, settings);
            });
        }

        private async Task SetStreamWallpaper(WallpaperSettings settings)
        {
            // Для стримов создаем HTML-плеер
            var htmlPath = CreateStreamPlayer(settings.FilePath, settings);

            // Запускаем браузер в кiosk режиме
            var startInfo = new ProcessStartInfo
            {
                FileName = "msedge.exe", // или chrome.exe
                Arguments = $"--kiosk --no-toolbar --no-location-bar --disable-infobars \"{htmlPath}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Maximized
            };

            try
            {
                _wallpaperProcess = Process.Start(startInfo);

                // Встраиваем окно браузера в рабочий стол
                await Task.Delay(2000); // Ждем загрузки браузера
                EmbedWindowInDesktop(_wallpaperProcess.MainWindowHandle);
            }
            catch
            {
                // Fallback на Chrome
                startInfo.FileName = "chrome.exe";
                _wallpaperProcess = Process.Start(startInfo);
                await Task.Delay(2000);
                EmbedWindowInDesktop(_wallpaperProcess.MainWindowHandle);
            }
        }

        private void CreateWallpaperWindow(string mediaPath, WallpaperSettings settings)
        {
            _wallpaperWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = false,
                ShowInTaskbar = false,
                Left = 0,
                Top = 0,
                Width = SystemParameters.PrimaryScreenWidth,
                Height = SystemParameters.PrimaryScreenHeight
            };

            _wallpaperPlayer = new MediaElement
            {
                Source = new Uri(mediaPath),
                Stretch = Stretch.UniformToFill,
                LoadedBehavior = MediaElementBehavior.Manual,
                UnloadedBehavior = MediaElementBehavior.Close,
                IsMuted = settings.Mute
            };

            if (settings.Loop)
            {
                _wallpaperPlayer.MediaEnded += (s, e) =>
                {
                    _wallpaperPlayer.Position = TimeSpan.Zero;
                    _wallpaperPlayer.Play();
                };
            }

            _wallpaperWindow.Content = _wallpaperPlayer;
            _wallpaperWindow.Show();

            // Встраиваем окно в рабочий стол
            EmbedWindowInDesktop(new System.Windows.Interop.WindowInteropHelper(_wallpaperWindow).Handle);

            _wallpaperPlayer.Play();
        }

        private void EmbedWindowInDesktop(IntPtr windowHandle)
        {
            // Получаем handle рабочего стола
            var progman = WinAPI.FindWindow("Progman", null);
            WinAPI.SendMessage(progman, 0x052C, new IntPtr(0x0000000D), IntPtr.Zero);
            WinAPI.SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(0));
            WinAPI.SendMessage(progman, 0x052C, new IntPtr(0x0000000D), new IntPtr(1));

            // Находим WorkerW окно
            var workerW = IntPtr.Zero;
            WinAPI.EnumWindows((topHandle, topParamHandle) =>
            {
                var p = WinAPI.FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);
                if (p != IntPtr.Zero)
                {
                    workerW = WinAPI.FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);

            // Встраиваем наше окно
            WinAPI.SetParent(windowHandle, workerW);
        }

        private string CreateStreamPlayer(string streamUrl, WallpaperSettings settings)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        * {{ margin: 0; padding: 0; }}
        body {{ 
            background: #000; 
            overflow: hidden; 
            font-family: Arial, sans-serif;
        }}
        .video-container {{
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        video {{
            width: 100%;
            height: 100%;
            object-fit: cover;
        }}
        .error {{
            color: white;
            text-align: center;
            font-size: 24px;
        }}
    </style>
</head>
<body>
    <div class='video-container'>
        <video id='stream' autoplay {(settings.Mute ? "muted" : "")} {(settings.Loop ? "loop" : "")}>
            <source src='{streamUrl}' type='video/mp4'>
            <source src='{streamUrl}' type='application/x-mpegURL'>
            <div class='error'>Не удалось загрузить стрим</div>
        </video>
    </div>
    
    <script>
        const video = document.getElementById('stream');
        video.addEventListener('error', function(e) {{
            console.error('Video error:', e);
            setTimeout(() => {{
                video.load();
            }}, 5000);
        }});
        
        // Автоповтор при ошибке
        video.addEventListener('ended', function() {{
            if ({settings.Loop.ToString().ToLower()}) {{
                video.currentTime = 0;
                video.play();
            }}
        }});
    </script>
</body>
</html>";

            var tempPath = Path.Combine(Path.GetTempPath(), "LiveWallpaperApp", "stream_player.html");
            File.WriteAllText(tempPath, html);
            return tempPath;
        }

        public void RemoveWallpaper()
        {
            try
            {
                // Останавливаем видео обои
                if (_wallpaperPlayer != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _wallpaperPlayer.Stop();
                        _wallpaperPlayer = null;
                    });
                }

                // Закрываем окно обоев
                if (_wallpaperWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _wallpaperWindow.Close();
                        _wallpaperWindow = null;
                    });
                }

                // Завершаем процесс браузера для стримов
                if (_wallpaperProcess != null && !_wallpaperProcess.HasExited)
                {
                    _wallpaperProcess.Kill();
                    _wallpaperProcess = null;
                }

                // Восстанавливаем стандартные обои Windows
                WinAPI.SystemParametersInfo(WinAPI.SPI_SETDESKWALLPAPER, 0,
                    null, WinAPI.SPIF_UPDATEINIFILE | WinAPI.SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка удаления обоев: {ex.Message}", ex);
            }
        }

        public void Cleanup()
        {
            RemoveWallpaper();
            _mediaProcessor?.CleanupTempFiles();
        }
    }

    public class WallpaperSettings
    {
        public string FilePath { get; set; }
        public MediaType MediaType { get; set; }
        public bool Loop { get; set; } = true;
        public bool Mute { get; set; } = true;
        public VideoQuality Quality { get; set; } = VideoQuality.Medium;
    }
}