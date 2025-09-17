# Live Wallpaper Manager

Современное приложение для создания живых обоев в Windows. Поддерживает видео, GIF, изображения и потоковое воспроизведение.

## 🎯 Возможности

- **Статичные обои**: JPG, PNG, BMP, TIFF, WebP
- **Видео обои**: MP4, AVI, MOV, WMV, MKV, WebM
- **GIF анимация**: с автоматической конвертацией
- **Потоковые обои**: RTMP, HLS, HTTP потоки
- **Интуитивный интерфейс**: современный Material Design
- **Оптимизация производительности**: настройка качества
- **Зацикленное воспроизведение**: без пауз
- **Управление звуком**: отключение аудио для видео

## 📋 Требования

- Windows 10/11
- .NET 6.0 Runtime
- Visual Studio 2022 (для сборки)
- FFmpeg (для обработки видео)

## ⚙️ Установка

### 1. Клонирование репозитория
```bash
git clone https://github.com/your-repo/live-wallpaper-app.git
cd live-wallpaper-app
```

### 2. Установка FFmpeg
Скачайте FFmpeg с https://ffmpeg.org/download.html и установите в `C:\ffmpeg\bin`

Или используйте Chocolatey:
```bash
choco install ffmpeg
```

### 3. Сборка проекта
```bash
dotnet build --configuration Release
```

### 4. Запуск
```bash
dotnet run
```

## 🚀 Использование

### Импорт медиа
1. Откройте приложение
2. Выберите тип медиа:
   - **📷 Изображение** - для статичных обоев
   - **🎬 Видео** - для видео обоев
   - **🎞️ GIF** - для анимированных обоев
   - **📺 Стрим** - для потокового контента

### Настройки
- **Зацикленное воспроизведение**: автоматический повтор
- **Отключить звук**: убрать аудио из видео
- **Качество**: выбор производительности vs качества

### Установка обоев
1. Выберите медиа файл
2. Настройте параметры
3. Нажмите **🖼️ Установить обои**

### Удаление обоев
Нажмите **🗑️ Удалить обои** для возврата к стандартным обоям Windows

## 📁 Структура проекта

```
LiveWallpaperApp/
├── Core/
│   ├── MediaProcessor.cs      # Обработка медиа файлов
│   └── WallpaperEngine.cs     # Управление обоями
├── UI/
│   ├── MainWindow.xaml        # Главное окно
│   ├── MainWindow.xaml.cs     # Логика главного окна
│   ├── StreamInputDialog.xaml # Диалог ввода стрима
│   └── StreamInputDialog.xaml.cs
├── Utils/
│   ├── WinAPI.cs             # Windows API функции
│   └── FileHandler.cs        # Работа с файлами
├── App.xaml                  # Ресурсы приложения
├── App.xaml.cs              # Инициализация приложения
└── LiveWallpaperApp.csproj   # Конфигурация проекта
```

## 🔧 Конфигурация

### Настройка FFmpeg
По умолчанию FFmpeg ищется в `C:\ffmpeg\bin`. Для изменения пути отредактируйте `App.xaml.cs`:

```csharp
FFMpegCore.GlobalFFOptions.Configure(options => 
    options.BinaryFolder = @"ВАШ_ПУТЬ_К_FFMPEG");
```

### Поддерживаемые форматы стримов
- HTTP/HTTPS потоки (.m3u8, .mp4)
- RTMP потоки
- WebRTC потоки

Примеры URL:
```
https://example.com/stream.m3u8
rtmp://live.twitch.tv/live/STREAM_KEY
https://www.youtube.com/watch?v=VIDEO_ID
```

## 🛠️ Разработка

### Добавление нового формата
1. Обновите `FileHandler.cs` с новыми расширениями
2. Добавьте обработку в `MediaProcessor.cs`
3. Обновите UI фильтры в `MainWindow.xaml.cs`

### Оптимизация производительности
- Настройка CRF (Constant Rate Factor) в `MediaProcessor.cs`
- Изменение разрешения выходного видео
- Настройка буферизации для стримов

## ⚠️ Известные ограничения

- Стримы требуют стабильного интернет-соединения
- Некоторые защищенные потоки могут не работать
- Высокое разрешение видео может влиять на производительность
- Требуется перезапуск для применения некоторых настроек

## 📝 Логи и отладка

Логи сохраняются в:
```
%TEMP%\LiveWallpaperApp\logs\
```

Временные файлы:
```
%TEMP%\LiveWallpaperApp\
```

## 🤝 Вклад в проект

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📄 Лицензия

Проект распространяется под лицензией MIT. См. `LICENSE` файл для деталей.

## 🆘 Поддержка

При возникновении проблем:
1. Проверьте установку FFmpeg
2. Убедитесь в поддержке формата файла
3. Проверьте права доступа к файлам
4. Создайте issue с описанием проблемы

---

**Live Wallpaper Manager** - делает ваш рабочий стол живым! 🎨