using System.Runtime.InteropServices;
using System.Diagnostics;

class WallpaperWindow
{
    private const uint SMTO_NORMAL = 0x0000;
    private const int WM_SHELLHOOK = 0x052C;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr p = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (p != IntPtr.Zero)
        {
            Marshal.WriteIntPtr(lParam, FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null));
        }
        return true;
    }

    public static IntPtr GetWallpaperWindow()
    {
        IntPtr progman = FindWindow("ProgMan", null);
        IntPtr result;
        SendMessageTimeout(progman, WM_SHELLHOOK, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out result);

        IntPtr wallpaperHwnd = IntPtr.Zero;
        IntPtr wallpaperHwndPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
        EnumWindows(new EnumWindowsProc(EnumWindowsCallback), wallpaperHwndPointer);
        wallpaperHwnd = Marshal.ReadIntPtr(wallpaperHwndPointer);
        Marshal.FreeHGlobal(wallpaperHwndPointer);

        return wallpaperHwnd;
    }
}

class Program
{
    // static NotifyIcon trayIcon = new NotifyIcon();
    static void Main(string[] args)
    {
        IntPtr hwnd = WallpaperWindow.GetWallpaperWindow();
        Console.WriteLine("Wallpaper Window Handle: " + hwnd.ToString("X"));
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "SoultideWallpaper.exe");

        Directory.SetCurrentDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));
        // var path = "./bin/SoultideWallpaper.exe";
        var cmdline = $"-parentHWND {hwnd}";//子ウィンドウとして起動
        Process exe = Process.Start(path, cmdline);

        // trayIcon.Text = "SoultideWallpaper";
        // trayIcon.Icon = SystemIcons.Application; // 標準のアイコンを使用

        // // タスクトレイのコンテキストメニューを作成
        // ContextMenuStrip contextMenu = new ContextMenuStrip();
        // ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);
        // contextMenu.Items.Add(exitMenuItem);

        // trayIcon.ContextMenuStrip = contextMenu;

        // タスクトレイにアイコンを表示
        // trayIcon.Visible = true;

    }
    // static void OnExit(object? sender, EventArgs e)
    // {
    //     trayIcon.Visible = false;
    //     Application.Exit();
    // }
}
