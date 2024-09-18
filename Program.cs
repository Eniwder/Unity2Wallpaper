using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
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
    static NotifyIcon trayIcon = new NotifyIcon();
    static Process? exeProcess = null;

    static void Main(string[] args)
    {
        IntPtr hwnd = WallpaperWindow.GetWallpaperWindow();
        Console.WriteLine("Wallpaper Window Handle: " + hwnd.ToString("X"));
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "SoultideWallpaper.exe");

        Directory.SetCurrentDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));
        var cmdline = $"-parentHWND {hwnd}";//子ウィンドウとして起動
        exeProcess = Process.Start(path, cmdline);

        trayIcon.Text = "SoultideWallpaper";
        string iconFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appicon.ico");
        if (File.Exists(iconFilePath))
        {
            using (FileStream fs = new FileStream(iconFilePath, FileMode.Open, FileAccess.Read))
            {
                trayIcon.Icon = new Icon(fs);
            }
        }
        else
        {
            Console.WriteLine($"Icon file not found: {iconFilePath}");
            trayIcon.Icon = SystemIcons.Application; // アイコンファイルが見つからない場合はデフォルトアイコンを使用
        }

        // // タスクトレイのコンテキストメニューを作成
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);
        contextMenu.Items.Add(exitMenuItem);

        trayIcon.ContextMenuStrip = contextMenu;

        // タスクトレイにアイコンを表示
        trayIcon.Visible = true;
        Application.Run();

    }
    static void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        if (exeProcess != null && !exeProcess.HasExited)
        {
            exeProcess.Kill(); // プロセスを強制終了
        }
        Application.Exit();
    }
}
