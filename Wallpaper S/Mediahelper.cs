using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

public static class MediaHelper
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".wmv", ".mov", ".flv", ".webm", ".m4v", ".3gp", ".ogv"
    };

    private static readonly HashSet<string> AnimationExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif", ".apng"
    };

    private static readonly HashSet<string> StreamProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "rtmp", "rtsp", "mms", "mmsh"
    };

    public enum MediaType
    {
        Unknown,
        StaticImage,
        AnimatedImage,
        Video,
        Stream
    }

    public static MediaType GetMediaType(string path)
    {
        if (string.IsNullOrEmpty(path))
            return MediaType.Unknown;

        // ���������, �������� �� ��� URL
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
        {
            if (StreamProtocols.Contains(uri.Scheme))
                return MediaType.Stream;
        }

        // ��������� ���������� �����
        string extension = Path.GetExtension(path);

        if (AnimationExtensions.Contains(extension))
            return MediaType.AnimatedImage;

        if (ImageExtensions.Contains(extension))
            return MediaType.StaticImage;

        if (VideoExtensions.Contains(extension))
            return MediaType.Video;

        return MediaType.Unknown;
    }

    public static bool IsValidMediaFile(string path)
    {
        return GetMediaType(path) != MediaType.Unknown;
    }

    public static bool IsStaticImage(string path)
    {
        return GetMediaType(path) == MediaType.StaticImage;
    }

    public static bool IsAnimatedMedia(string path)
    {
        var type = GetMediaType(path);
        return type == MediaType.AnimatedImage || type == MediaType.Video || type == MediaType.Stream;
    }

    public static string GetFileTypeDescription(string path)
    {
        return GetMediaType(path) switch
        {
            MediaType.StaticImage => "����������� �����������",
            MediaType.AnimatedImage => "������������� �����������",
            MediaType.Video => "����� ����",
            MediaType.Stream => "���-�����",
            _ => "����������� ���"
        };
    }

    public static Size? GetImageDimensions(string imagePath)
    {
        try
        {
            using var image = Image.FromFile(imagePath);
            return new Size(image.Width, image.Height);
        }
        catch
        {
            return null;
        }
    }

    public static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return string.Format("{0:n1} {1}", number, suffixes[counter]);
    }

    public static string GetFileInfo(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "���� �� ������";

        try
        {
            var mediaType = GetMediaType(path);

            if (mediaType == MediaType.Stream)
            {
                return $"�����: {path}";
            }

            if (!File.Exists(path))
                return "���� �� ������";

            var fileInfo = new FileInfo(path);
            var size = FormatFileSize(fileInfo.Length);
            var typeDescription = GetFileTypeDescription(path);

            string result = $"{typeDescription}\n������: {size}";

            if (mediaType == MediaType.StaticImage)
            {
                var dimensions = GetImageDimensions(path);
                if (dimensions.HasValue)
                {
                    result += $"\n����������: {dimensions.Value.Width}x{dimensions.Value.Height}";
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"������: {ex.Message}";
        }
    }

    public static string GetOpenFileDialogFilter()
    {
        var allSupported = string.Join(";",
            ImageExtensions.Select(e => "*" + e)
            .Concat(VideoExtensions.Select(e => "*" + e))
            .Concat(AnimationExtensions.Select(e => "*" + e)));

        return $"��� ��������������|{allSupported}|" +
               $"�����������|{string.Join(";", ImageExtensions.Select(e => "*" + e))}|" +
               $"�����|{string.Join(";", VideoExtensions.Select(e => "*" + e))}|" +
               $"��������|{string.Join(";", AnimationExtensions.Select(e => "*" + e))}|" +
               "��� �����|*.*";
    }

    public static bool ValidateStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return false;

        return StreamProtocols.Contains(uri.Scheme);
    }

    public static string NormalizeStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        url = url.Trim();

        // ��������� http:// ���� �������� �� ������
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }

        return url;
    }
}