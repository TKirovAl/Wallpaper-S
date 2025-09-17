using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiveWallpaperApp.Core;

namespace LiveWallpaperApp.Utils
{
    public static class FileHandler
    {
        // Поддерживаемые форматы изображений
        public static readonly string[] ImageExtensions = {
            ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp"
        };

        // Поддерживаемые форматы видео
        public static readonly string[] VideoExtensions = {
            ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".webm", ".flv", ".m4v", ".3gp"
        };

        // Поддерживаемые форматы GIF
        public static readonly string[] GifExtensions = {
            ".gif"
        };

        // Все поддерживаемые форматы
        public static readonly string[] AllSupportedExtensions =
            ImageExtensions.Concat(VideoExtensions).Concat(GifExtensions).ToArray();

        /// <summary>
        /// Определяет тип медиа по расширению файла
        /// </summary>
        public static MediaType GetMediaType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (ImageExtensions.Contains(extension))
                return MediaType.Image;

            if (VideoExtensions.Contains(extension))
                return MediaType.Video;

            if (GifExtensions.Contains(extension))
                return MediaType.Gif;

            throw new NotSupportedException($"Формат файла {extension} не поддерживается");
        }

        /// <summary>
        /// Проверяет, поддерживается ли формат файла
        /// </summary>
        public static bool IsSupported(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return AllSupportedExtensions.Contains(extension);
        }

        /// <summary>
        /// Получает фильтр для диалога открытия файлов
        /// </summary>
        public static string GetOpenFileFilter(MediaType? mediaType = null)
        {
            if (!mediaType.HasValue)
            {
                return "Все медиа файлы|" + string.Join(";", AllSupportedExtensions.Select(ext => $"*{ext}")) +
                       "|Изображения|" + string.Join(";", ImageExtensions.Select(ext => $"*{ext}")) +
                       "|Видео|" + string.Join(";", VideoExtensions.Select(ext => $"*{ext}")) +
                       "|GIF|*.gif" +
                       "|Все файлы|*.*";
            }

            return mediaType.Value switch
            {
                MediaType.Image => "Изображения|" + string.Join(";", ImageExtensions.Select(ext => $"*{ext}")) + "|Все файлы|*.*",
                MediaType.Video => "Видео|" + string.Join(";", VideoExtensions.Select(ext => $"*{ext}")) + "|Все файлы|*.*",
                MediaType.Gif => "GIF файлы|*.gif|Все файлы|*.*",
                _ => "Все файлы|*.*"
            };
        }

        /// <summary>
        /// Проверяет существование и доступность файла
        /// </summary>
        public static void ValidateFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            if (!IsSupported(filePath))
                throw new NotSupportedException($"Формат файла не поддерживается: {Path.GetExtension(filePath)}");

            // Проверяем доступность файла для чтения
            try
            {
                using var stream = File.OpenRead(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Нет доступа к файлу: {filePath}");
            }
            catch (IOException ex)
            {
                throw new IOException($"Ошибка доступа к файлу: {filePath}. {ex.Message}");
            }
        }

        /// <summary>
        /// Получает безопасное имя файла для сохранения
        /// </summary>
        public static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeFileName = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

            // Ограничиваем длину имени файла
            if (safeFileName.Length > 100)
                safeFileName = safeFileName.Substring(0, 100);

            return safeFileName;
        }

        /// <summary>
        /// Получает размер файла в человекочитаемом формате
        /// </summary>
        public static string GetFileSizeString(string filePath)
        {
            if (!File.Exists(filePath))
                return "Unknown";

            var fileInfo = new FileInfo(filePath);
            return GetFileSizeString(fileInfo.Length);
        }

        /// <summary>
        /// Конвертирует размер в байтах в человекочитаемый формат
        /// </summary>
        public static string GetFileSizeString(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Создает резервную копию файла
        /// </summary>
        public static string CreateBackup(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var backupPath = Path.Combine(directory, $"{fileName}_backup_{timestamp}{extension}");
            File.Copy(filePath, backupPath);

            return backupPath;
        }

        /// <summary>
        /// Очищает временные файлы старше указанного времени
        /// </summary>
        public static void CleanupTempFiles(string tempDirectory, TimeSpan maxAge)
        {
            if (!Directory.Exists(tempDirectory))
                return;

            try
            {
                var files = Directory.GetFiles(tempDirectory);
                var cutoffTime = DateTime.Now - maxAge;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffTime)
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

        /// <summary>
        /// Проверяет, является ли URL валидным для стрима
        /// </summary>
        public static bool IsValidStreamUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "rtmp" || uri.Scheme == "rtmps";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Получает MIME тип файла по расширению
        /// </summary>
        public static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",
                ".flv" => "video/x-flv",
                _ => "application/octet-stream"
            };
        }
    }
}