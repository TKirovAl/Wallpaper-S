using System;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;

public static class WallpaperAPI
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;
    private const uint WM_COMMAND = 0x111;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    public static bool SetStaticWallpaper(string imagePath)
    {
        if (!File.Exists(imagePath))
            return false;

        try
        {
            // ��������� ����������� �����
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if (key != null)
                {
                    key.SetValue("WallpaperStyle", "10"); // ��������� �����
                    key.SetValue("TileWallpaper", "0");   // �� �������
                }
            }

            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE) != 0;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"������ ��������� �����: {ex.Message}", "������");
            return false;
        }
    }

    public static IntPtr GetWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            throw new InvalidOperationException("�� ������� ����� ���� Progman");
        }

        // ���������� ��������� ��� �������� WorkerW
        IntPtr result;
        SendMessageTimeout(progman, WM_COMMAND, (IntPtr)0x052C, IntPtr.Zero, 0x0, 1000, out result);

        // ���� ������� ��� �������� WorkerW
        System.Threading.Thread.Sleep(100);

        IntPtr workerw = IntPtr.Zero;
        EnumWindows((topHandle, topParamHandle) =>
        {
            IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerw = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                if (workerw != IntPtr.Zero)
                {
                    return false; // ������������� �����
                }
            }
            return true;
        }, IntPtr.Zero);

        if (workerw == IntPtr.Zero)
        {
            // �������������� ����� ������
            workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
        }

        return workerw;
    }

    public static void SetParentToDesktop(IntPtr windowHandle)
    {
        try
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("�������� handle ����");
            }

            // ����� 1: ������� ���������� � WorkerW
            bool integrated = TryIntegrateWithWorkerW(windowHandle);

            if (!integrated)
            {
                // ����� 2: �������������� ����������
                TryAlternativeIntegration(windowHandle);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"������ ���������� � ������� ������: {ex.Message}", ex);
        }
    }

    private static bool TryIntegrateWithWorkerW(IntPtr windowHandle)
    {
        try
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero) return false;

            // ���������� ������� ��� �������� WorkerW
            SendMessageTimeout(progman, WM_COMMAND, (IntPtr)0x052C, IntPtr.Zero, 0x0, 1000, out _);
            System.Threading.Thread.Sleep(100);

            // ���� WorkerW � SHELLDLL_DefView
            IntPtr workerw = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                IntPtr shellView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    workerw = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                    if (workerw != IntPtr.Zero)
                        return false; // ������, ������������� �����
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
                IntPtr result = SetParent(windowHandle, workerw);
                if (result != IntPtr.Zero)
                {
                    ConfigureWallpaperWindow(windowHandle);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void TryAlternativeIntegration(IntPtr windowHandle)
    {
        try
        {
            // ����� ����������� ���������� �� ������ �����
            ConfigureWallpaperWindow(windowHandle);

            // ��������� ������� ��������� ���� �����
            for (int i = 0; i < 3; i++)
            {
                SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                System.Threading.Thread.Sleep(50);
            }

            // ������� �� ������ ����� � Alt+Tab
            int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
            SetWindowLong(windowHandle, GWL_EXSTYLE,
                exStyle | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            // �� ���������� ��������� - ������������ ��� ����� � ��������
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"�������������� ���������� �� �������: {ex.Message}");
        }
    }

    private static void ConfigureWallpaperWindow(IntPtr windowHandle)
    {
        // ������ ���� ���������� ��� ����
        int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
        SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

        // ��������� �� ������ �����
        SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    public static void RestoreDefaultWallpaper()
    {
        try
        {
            // �������� ���� � ����������� ����� Windows
            string defaultWallpaper = GetDefaultWallpaperPath();

            if (!string.IsNullOrEmpty(defaultWallpaper) && File.Exists(defaultWallpaper))
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, defaultWallpaper, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            else
            {
                // ���� ����������� ���� �� �������, ������������� �������� ����
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"������ �������������� �����: {ex.Message}", "������");
        }
    }

    private static string GetDefaultWallpaperPath()
    {
        try
        {
            // ���� � ����������� ����� Windows
            string[] defaultPaths = {
                @"C:\Windows\Web\Wallpaper\Windows\img0.jpg",
                @"C:\Windows\Web\Wallpaper\Theme1\img1.jpg",
                @"C:\Windows\Web\Wallpaper\Theme2\img1.jpg",
                @"C:\Windows\Web\Screen\img100.jpg",
                Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\Microsoft\Windows\Themes\TranscodedWallpaper")
            };

            foreach (string path in defaultPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // �������� ������� ���� �� �������
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
            {
                if (key != null)
                {
                    string currentWallpaper = key.GetValue("Wallpaper")?.ToString();
                    if (!string.IsNullOrEmpty(currentWallpaper) && File.Exists(currentWallpaper))
                        return currentWallpaper;
                }
            }
        }
        catch
        {
            // ���������� ������
        }

        return string.Empty;
    }
}