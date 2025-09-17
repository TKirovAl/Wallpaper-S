using System;
using System.IO;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;

namespace LiveWallpaperApp.Core
{
    public class MediaProcessor
    {
        private readonly string _tempDirectory;

        public MediaProcessor()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "LiveWallpaperApp");
            if (!Directory.Exists(_tempDirectory))
                Directory.CreateDirectory(_tempDirectory);
        }

        public async Task<string> ProcessVideoForWallpaper(string inputPath, VideoQuality quality, bool mute)
        {
            var outputPath = Path.Combine(_tempDirectory,
                $"processed_{Guid.NewGuid()}.mp4");

            try
            {
                var conversion = FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithConstantRateFactor(GetCRF(quality))
                            .WithVariableBitrate(4)
                            .WithVideoFilters(filterOptions => filterOptions
                                .Scale(VideoSize.Hd))
                            .WithFastStart();

                        if (mute)
                            options.WithoutAudio();
                        else
                            options.WithAudioCodec(AudioCodec.Aac);
                    });

                await conversion.ProcessAsynchronously();
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обработки видео: {ex.Message}", ex);
            }
        }

        public async Task<string> ProcessGifForWallpaper(string inputPath, VideoQuality quality)
        {
            var outputPath = Path.Combine(_tempDirectory,
                $"processed_{Guid.NewGuid()}.mp4");

            try
            {
                var conversion = FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithConstantRateFactor(GetCRF(quality))
                            .Loop(-1)
                            .WithFastStart();
                    });

                await conversion.ProcessAsynchronously();
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обработки GIF: {ex.Message}", ex);
            }
        }

        public string ProcessImageForWallpaper(string inputPath, VideoQuality quality)
        {
            // Для статичных изображений просто копируем в временную папку
            var extension = Path.GetExtension(inputPath);
            var outputPath = Path.Combine(_tempDirectory,
                $"processed_{Guid.NewGuid()}{extension}");

            File.Copy(inputPath, outputPath, true);
            return outputPath;
        }

        public async Task<bool> ValidateMediaFile(string filePath)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                return mediaInfo != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<MediaInfo> GetMediaInfo(string filePath)
        {
            try
            {
                var info = await FFProbe.AnalyseAsync(filePath);
                return new MediaInfo
                {
                    Duration = info.Duration,
                    Width = info.PrimaryVideoStream?.Width ?? 0,
                    Height = info.PrimaryVideoStream?.Height ?? 0,
                    HasAudio = info.PrimaryAudioStream != null,
                    Format = info.Format.FormatName
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения информации о медиа: {ex.Message}", ex);
            }
        }

        private int GetCRF(VideoQuality quality)
        {
            return quality switch
            {
                VideoQuality.Low => 28,
                VideoQuality.Medium => 23,
                VideoQuality.High => 18,
                _ => 23
            };
        }

        public void CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    var files = Directory.GetFiles(_tempDirectory);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Игнорируем ошибки удаления
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }

    public class MediaInfo
    {
        public TimeSpan Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool HasAudio { get; set; }
        public string Format { get; set; }
    }

    public enum MediaType
    {
        Image,
        Video,
        Gif,
        Stream
    }

    public enum VideoQuality
    {
        Low,
        Medium,
        High
    }
}