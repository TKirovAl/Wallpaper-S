using System;
using System.Runtime.InteropServices;
using System.Drawing;
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;
    private const uint WM_COMMAND = 0x111;

    public static bool SetStaticWallpaper(string imagePath)
    {
        if (!File.Exists(imagePath))
            return false;

        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key?.SetValue(@"WallpaperStyle", "10");
            key?.SetValue(@"TileWallpaper", "0");
            key?.Close();

            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE) != 0;
        }
        catch
        {
            return false;
        }
    }

    public static IntPtr GetWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, WM_COMMAND, (IntPtr)0x052C, IntPtr.Zero, 0x0, 1000, out _);

        IntPtr workerw = IntPtr.Zero;
        EnumWindows((topHandle, topParamHandle) =>
        {
            IntPtr p = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);
            if (p != IntPtr.Zero)
            {
                workerw = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", IntPtr.Zero);
            }
            return true;
        }, IntPtr.Zero);

        return workerw;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public static void SetParentToDesktop(IntPtr windowHandle)
    {
        IntPtr workerw = GetWorkerW();
        SetParent(windowHandle, workerw);
    }
}