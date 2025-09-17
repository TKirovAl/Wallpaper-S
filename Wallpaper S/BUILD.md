# Инструкции по сборке и установке

## 📋 Пререквизиты

### Системные требования
- Windows 10 версия 1903 или новее
- .NET 6.0 или новее
- Visual Studio 2022 или Visual Studio Code
- Минимум 100 МБ свободного места на диске

### Зависимости
- FFmpeg (для обработки видео)
- Microsoft Edge WebView2 (обычно уже установлен)

## 🛠️ Пошаговая установка

### 1. Клонирование репозитория
```bash
git clone <repository-url>
cd LiveWallpaperApp
```

### 2. Установка .NET 6.0
Скачайте и установите .NET 6.0 SDK:
https://dotnet.microsoft.com/download/dotnet/6.0

Проверьте установку:
```bash
dotnet --version
```

### 3. Установка FFmpeg

#### Способ 1: Ручная установка
1. Скачайте FFmpeg: https://www.gyan.dev/ffmpeg/builds/
2. Извлеките в `C:\ffmpeg`
3. Добавьте `C:\ffmpeg\bin` в PATH

#### Способ 2: Chocolatey (рекомендуется)
```powershell
# Установка Chocolatey (если не установлен)
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

# Установка FFmpeg
choco install ffmpeg
```

#### Способ 3: Winget
```powershell
winget install FFmpeg
```

### 4. Проверка FFmpeg
```bash
ffmpeg -version
```

## 🔨 Сборка проекта

### Сборка в Debug режиме
```bash
dotnet build
```

### Сборка в Release режиме
```bash
dotnet build --configuration Release
```

### Запуск проекта
```bash
dotnet run
```

### Публикация приложения
```bash
# Для Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Для создания single-file executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 📦 Создание установщика

### Способ 1: WiX Toolset (рекомендуется)

1. Установите WiX Toolset:
```bash
dotnet tool install --global wix
```

2. Создайте файл `setup.wxs`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="Live Wallpaper Manager" Language="1033" 
           Version="1.0.0.0" Manufacturer="Your Name" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="Live Wallpaper Manager" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>
  
  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="LiveWallpaperManager" />
      </Directory>
    </Directory>
  </Fragment>
  
  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="MainExecutable" Guid="*">
        <File Id="MainExe" Source="bin/Release/net6.0-windows/LiveWallpaperApp.exe" />
      </Component>
    </ComponentGroup>
  </Fragment>
</Wix>
```

3. Соберите MSI:
```bash
wix build setup.wxs
```

### Способ 2: NSIS (альтернатива)

1. Установите NSIS
2. Создайте скрипт установки `installer.nsi`
3. Компилируйте установщик

## 🚀 Автоматизация сборки

### GitHub Actions
Создайте `.github/workflows/build.yml`:
```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 6.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: LiveWallpaperApp
        path: bin/Release/net6.0-windows/win-x64/publish/
```

## 🔧 Конфигурация для разработки

### Visual Studio 2022
1. Откройте `LiveWallpaperApp.sln`
2. Установите стартовый проект
3. Выберите конфигурацию Debug/Release
4. Нажмите F5 для запуска

### Visual Studio Code
1. Установите расширения:
   - C# Dev Kit
   - .NET Runtime Install Tool
   
2. Откройте папку проекта
3. Настройте `launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/bin/Debug/net6.0-windows/LiveWallpaperApp.exe",
      "args": [],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole",
      "stopAtEntry": false
    }
  ]
}
```

## 🐛 Отладка

### Включение подробного логирования
Добавьте в `App.xaml.cs`:
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Включить логирование FFmpeg
    FFMpegCore.GlobalFFOptions.Configure(options => 
    {
        options.BinaryFolder = @"C:\ffmpeg\bin";
        options.VerboseLevel = VerboseLevel.Debug;
    });
    
    base.OnStartup(e);
}
```

### Проверка зависимостей
```csharp
public static void CheckDependencies()
{
    // Проверка FFmpeg
    if (!FFMpegCore.FFMpegOptions.Options.BinaryFolder.Exists)
    {
        MessageBox.Show("FFmpeg не найден!");
    }
    
    // Проверка .NET версии
    var version = Environment.Version;
    Console.WriteLine($".NET версия: {version}");
}
```

## 📋 Чек-лист перед релизом

- [ ] Все тесты проходят
- [ ] FFmpeg работает корректно
- [ ] Обрабатываются все поддерживаемые форматы
- [ ] UI адаптирован для разных разрешений
- [ ] Временные файлы очищаются
- [ ] Настройки сохраняются и загружаются
- [ ] Приложение корректно завершает работу
- [ ] Установщик работает на чистой системе
- [ ] Документация актуальна

## ❗ Распространенные проблемы

### FFmpeg не найден
```
Решение: Убедитесь что FFmpeg установлен и доступен в PATH
```

### Ошибка сборки проекта
```
Решение: Проверьте версию .NET SDK и восстановите пакеты:
dotnet restore --force
```

### Проблемы с правами доступа
```
Решение: Запустите Visual Studio от имени администратора
```

### Медленная обработка видео
```
Решение: Включите аппаратное ускорение в настройках приложения
```

## 📞 Поддержка

При возникновении проблем:
1. Проверьте FAQ в README.md
2. Создайте Issue в GitHub
3. Приложите логи из `%TEMP%/LiveWallpaperApp/logs/`