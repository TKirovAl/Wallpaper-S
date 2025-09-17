using System;
using System.IO;
using System.Text.Json;
using FFMpegCore;
using LiveWallpaperApp.Core;

namespace LiveWallpaperApp.Config
{
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly object _lock = new object();
        private readonly string _settingsPath;

        public static AppSettings Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new AppSettings();
                }
            }
        }

        private AppSettings()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "LiveWallpaperApp");

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _settingsPath = Path.Combine(appFolder, "settings.json");
            LoadSettings();
        }

        // Настройки по умолчанию
        public string FFmpegPath { get; set; } = @"C:\ffmpeg\bin";
        public VideoQuality DefaultQuality { get; set; } = VideoQuality.Medium;
        public bool DefaultLoop { get; set; } = true;
        public bool DefaultMute { get; set; } = true;
        public int MaxConcurrentProcessing { get; set; } = 2;
        public int TempFileRetentionDays { get; set; } = 7;
        public bool AutoStartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public string LastUsedDirectory { get; set; } = "";
        public string Language { get; set; } = "ru-RU";
        public bool CheckForUpdates { get; set; } = true;
        public int StreamBufferSize { get; set; } = 1024 * 1024; // 1MB
        public int StreamTimeoutSeconds { get; set; } = 30;
        public bool HardwareAcceleration { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public WindowSettings MainWindow { get; set; } = new WindowSettings();

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                        CopyFrom(settings);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки загрузки настроек
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                // Используем настройки по умолчанию
            }
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void CopyFrom(AppSettings source)
        {
            FFmpegPath = source.FFmpegPath;
            DefaultQuality = source.DefaultQuality;
            DefaultLoop = source.DefaultLoop;
            DefaultMute = source.DefaultMute;
            MaxConcurrentProcessing = source.MaxConcurrentProcessing;
            TempFileRetentionDays = source.TempFileRetentionDays;
            AutoStartWithWindows = source.AutoStartWithWindows;
            MinimizeToTray = source.MinimizeToTray;
            LastUsedDirectory = source.LastUsedDirectory;
            Language = source.Language;
            CheckForUpdates = source.CheckForUpdates;
            StreamBufferSize = source.StreamBufferSize;
            StreamTimeoutSeconds = source.StreamTimeoutSeconds;
            HardwareAcceleration = source.HardwareAcceleration;
            ShowNotifications = source.ShowNotifications;
            MainWindow = source.MainWindow ?? new WindowSettings();
        }

        public void ResetToDefaults()
        {
            var defaults = new AppSettings();
            CopyFrom(defaults);
            SaveSettings();
        }

        public bool ValidateFFmpegPath()
        {
            if (string.IsNullOrEmpty(FFmpegPath))
                return false;

            var ffmpegExe = Path.Combine(FFmpegPath, "ffmpeg.exe");
            return File.Exists(ffmpegExe);
        }

        public string GetTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "LiveWallpaperApp");
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }

    public class WindowSettings
    {
        public double Width { get; set; } = 900;
        public double Height { get; set; } = 600;
        public double Left { get; set; } = -1; // -1 означает центрирование
        public double Top { get; set; } = -1;
        public bool IsMaximized { get; set; } = false;
    }

    public class ProcessingSettings
    {
        public bool UseHardwareAcceleration { get; set; } = true;
        public int MaxFrameRate { get; set; } = 60;
        public int MaxWidth { get; set; } = 3840; // 4K
        public int MaxHeight { get; set; } = 2160;
        public string VideoCodec { get; set; } = "libx264";
        public string AudioCodec { get; set; } = "aac";
        public int AudioBitrate { get; set; } = 128; // kbps
        public bool PreserveAspectRatio { get; set; } = true;
    }

    public static class SettingsManager
    {
        public static void Initialize()
        {
            var settings = AppSettings.Instance;

            // Применяем настройки FFmpeg
            if (settings.ValidateFFmpegPath())
            {
                FFMpegCore.GlobalFFOptions.Configure(options =>
                    options.BinaryFolder = settings.FFmpegPath);
            }
            else
            {
                // Пытаемся найти FFmpeg в PATH
                TryAutoDetectFFmpeg();
            }
        }

        private static void TryAutoDetectFFmpeg()
        {
            var pathVariables = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();

            foreach (var path in pathVariables)
            {
                if (string.IsNullOrEmpty(path)) continue;

                var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(ffmpegPath))
                {
                    AppSettings.Instance.FFmpegPath = path;
                    AppSettings.Instance.SaveSettings();

                    FFMpegCore.GlobalFFOptions.Configure(options =>
                        options.BinaryFolder = path);
                    return;
                }
            }
        }

        public static void CleanupTempFiles()
        {
            var settings = AppSettings.Instance;
            var tempDir = settings.GetTempDirectory();

            if (!Directory.Exists(tempDir))
                return;

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-settings.TempFileRetentionDays);
                var files = Directory.GetFiles(tempDir);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки удаления отдельных файлов
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }
}