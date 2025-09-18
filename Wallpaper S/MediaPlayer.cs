using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace LiveWallpaperApp
{
    public class WallpaperMediaPlayer : IDisposable
    {
        private Window? wallpaperWindow;
        private MediaElement? mediaElement;
        private string mediaPath;
        private bool isDisposed = false;
        private DispatcherTimer? reconnectTimer;

        public WallpaperMediaPlayer(string path)
        {
            mediaPath = path;
        }

        public bool Start()
        {
            try
            {
                CreateWallpaperWindow();
                SetupMediaElement();
                IntegrateWithDesktop();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска медиаплеера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CreateWallpaperWindow()
        {
            wallpaperWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = false,
                Background = Brushes.Black,
                Topmost = false,
                ShowInTaskbar = false,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight
            };

            wallpaperWindow.Show();
            wallpaperWindow.WindowState = WindowState.Maximized;

            // Событие после загрузки окна
            wallpaperWindow.Loaded += WallpaperWindow_Loaded;
        }

        private void WallpaperWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(wallpaperWindow).Handle;
                if (handle != IntPtr.Zero)
                {
                    SendWindowToBack(handle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке окна назад: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetupMediaElement()
        {
            mediaElement = new MediaElement
            {
                Stretch = Stretch.UniformToFill,
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Close,
                Volume = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            mediaElement.MediaOpened += OnMediaOpened;
            mediaElement.MediaEnded += OnMediaEnded;
            mediaElement.MediaFailed += OnMediaFailed;

            if (Uri.IsWellFormedUriString(mediaPath, UriKind.Absolute))
            {
                mediaElement.Source = new Uri(mediaPath);
            }
            else if (File.Exists(mediaPath))
            {
                mediaElement.Source = new Uri(mediaPath, UriKind.Absolute);
            }
            else
            {
                throw new FileNotFoundException($"Файл не найден: {mediaPath}");
            }

            wallpaperWindow!.Content = mediaElement;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            mediaElement?.Play();
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            if (mediaElement != null && !isDisposed)
            {
                mediaElement.Position = TimeSpan.Zero;
                mediaElement.Play();
            }
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (Uri.IsWellFormedUriString(mediaPath, UriKind.Absolute))
            {
                StartReconnectTimer();
            }
        }

        private void StartReconnectTimer()
        {
            reconnectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            reconnectTimer.Tick += (s, e) =>
            {
                try
                {
                    if (mediaElement != null)
                    {
                        mediaElement.Source = new Uri(mediaPath);
                        reconnectTimer.Stop();
                    }
                }
                catch
                {
                    // Продолжаем попытки
                }
            };
            reconnectTimer.Start();
        }

        private void IntegrateWithDesktop()
        {
            if (wallpaperWindow == null) return;

            try
            {
                var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(wallpaperWindow);
                IntPtr windowHandle = windowInteropHelper.Handle;

                if (windowHandle == IntPtr.Zero)
                {
                    wallpaperWindow.Loaded += (s, e) =>
                    {
                        try
                        {
                            windowHandle = new System.Windows.Interop.WindowInteropHelper(wallpaperWindow).Handle;
                            if (windowHandle != IntPtr.Zero)
                            {
                                PerformIntegration(windowHandle);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка интеграции: {ex.Message}",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    };
                }
                else
                {
                    PerformIntegration(windowHandle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при интеграции: {ex.Message}",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PerformIntegration(IntPtr windowHandle)
        {
            try
            {
                // Попытка интеграции с WorkerW
                WallpaperAPI.SetParentToDesktop(windowHandle);
                HideFromAltTab(windowHandle);
            }
            catch
            {
                // Альтернативное размещение
                SendWindowToBack(windowHandle);
                HideFromAltTab(windowHandle);
            }
        }

        private void SendWindowToBack(IntPtr windowHandle)
        {
            try
            {
                // Отправляем окно на задний план
                SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                // Делаем прозрачным для мыши
                int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                SetWindowLong(windowHandle, GWL_EXSTYLE,
                    exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);

                // Убираем фокус
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (wallpaperWindow != null)
                    {
                        wallpaperWindow.Topmost = false;
                        wallpaperWindow.WindowState = WindowState.Normal;
                        wallpaperWindow.WindowState = WindowState.Maximized;
                    }
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки окна назад: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HideFromAltTab(IntPtr windowHandle)
        {
            try
            {
                int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        public void Stop()
        {
            try
            {
                reconnectTimer?.Stop();
                reconnectTimer = null;

                mediaElement?.Stop();
                wallpaperWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка остановки плеера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                Stop();

                mediaElement = null;
                wallpaperWindow = null;

                isDisposed = true;
            }
        }

        // Windows API
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
    }
}