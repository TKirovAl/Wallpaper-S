using System;
using System.Runtime.InteropServices;

namespace LiveWallpaperApp.Utils
{
    public static class WinAPI
    {
        public const int SPI_SETDESKWALLPAPER = 0x0014;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam,
            string pvParam, int fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, IntPtr lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y,
            int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute,
            ref bool pvAttribute, uint cbAttribute);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        // Константы для SetWindowPos
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;

        // Константы для ShowWindow
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;

        // DWM атрибуты
        public const uint DWMWA_EXCLUDED_FROM_PEEK = 12;
        public const uint DWMWA_DISALLOW_PEEK = 13;

        /// <summary>
        /// Получает handle окна рабочего стола для встраивания живых обоев
        /// </summary>
        public static IntPtr GetDesktopWorkerWindow()
        {
            var progman = FindWindow("Progman", null);
            SendMessage(progman, 0x052C, new IntPtr(0x0000000D), IntPtr.Zero);

            IntPtr workerW = IntPtr.Zero;
            EnumWindows((topHandle, topParamHandle) =>
            {
                var p = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);
                if (p != IntPtr.Zero)
                {
                    workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", IntPtr.Zero);
                    return false; // Останавливаем поиск
                }
                return true;
            }, IntPtr.Zero);

            return workerW;
        }

        /// <summary>
        /// Проверяет, является ли окно видимым и действительным
        /// </summary>
        public static bool IsValidWindow(IntPtr hWnd)
        {
            return hWnd != IntPtr.Zero && IsWindowVisible(hWnd);
        }

        /// <summary>
        /// Получает заголовок окна
        /// </summary>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            if (!IsValidWindow(hWnd))
                return string.Empty;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return string.Empty;

            var buffer = new System.Text.StringBuilder(length + 1);
            GetWindowText(hWnd, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        /// <summary>
        /// Делает окно исключенным из Peek (Alt+Tab)
        /// </summary>
        public static void ExcludeFromPeek(IntPtr hWnd)
        {
            bool exclude = true;
            DwmSetWindowAttribute(hWnd, DWMWA_EXCLUDED_FROM_PEEK, ref exclude, sizeof(bool));
        }
    }
}