using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KillerOfUnwantedWindows1C
{
    public static class WinApi
    {
        #region user32

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LastInputInfo plii);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        #endregion

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        public struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }

        public const UInt32 WM_CLOSE = 0x0010;
        public const int SW_HIDE = 0;
    }
}