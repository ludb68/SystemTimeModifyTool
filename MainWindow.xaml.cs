using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Printing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace SystemTimeModifyTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private int _duration = 2;

    public MainWindow()
    {
        CheckWindows();

        RequireAdministrator();

        InitializeComponent();

        Init();
    }

    private static void RequireAdministrator()
    {
        if (new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator)) return;
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "RunAs"
            };
            Process.Start(
                processStartInfo);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }

        Application.Current.Shutdown();
    }

    private static void CheckWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        MessageBox.Show("仅支持Windows", "仅支持Windows", MessageBoxButton.OK);
        Application.Current.Shutdown();
    }

    private async void Init()
    {
        SystemTime.Text = DateTime.Now.ToString("G");

        NetworkTime.Text = (await GetNetworkTime()).ToString(CultureInfo.CurrentCulture);
    }

    private void DurationChange(object sender, RoutedEventArgs e)
    {
        var stringDuration = ((RadioButton)sender).Content.ToString()?.Split("小时每次")[0];
        _duration = int.Parse(stringDuration ?? "2");
        DurationDisplay.Text = _duration.ToString();
    }


    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        PInvoke.UnregisterHotKey(new HWND(handle), 1);
        PInvoke.UnregisterHotKey(new HWND(handle), 2);
        PInvoke.UnregisterHotKey(new HWND(handle), 3);
    }

    private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
        var hWnd = new HWND(handle);
        PInvoke.RegisterHotKey(hWnd, 1, HOT_KEY_MODIFIERS.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.Q));
        PInvoke.RegisterHotKey(hWnd, 2, HOT_KEY_MODIFIERS.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.A));
        PInvoke.RegisterHotKey(hWnd, 3, HOT_KEY_MODIFIERS.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.S));
    }

    private static readonly HttpClient SharedClient = new()
    {
        BaseAddress = new Uri("https://www.baidu.com/"),
        DefaultRequestHeaders =
        {
            Accept = { new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json) }
        }
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        switch (msg)
        {
            case (int)PInvoke.WM_HOTKEY:
                switch (wparam.ToInt32())
                {
                    case 1:
                        SetSystemTime(DateTime.Now.AddHours(_duration));

                        break;
                    case 2:
                        SetSystemTime(DateTime.Now.AddHours(-_duration));

                        break;
                    case 3:
                        SyncTime();

                        break;
                }

                break;
        }



        return IntPtr.Zero;
    }

    private void SetSystemTime(DateTime dateTime)
    {
        PInvoke.SetLocalTime(new SYSTEMTIME()
        {
            wYear = (ushort)dateTime.Year,
            wMonth = (ushort)dateTime.Month,
            wDay = (ushort)dateTime.Day,
            wHour = (ushort)dateTime.Hour,
            wMinute = (ushort)dateTime.Minute,
            wSecond = (ushort)dateTime.Second,
        });
        SystemTime.Text = dateTime.ToString(CultureInfo.CurrentCulture);
    }

    private async void SyncTime()
    {
        var dateTime = await GetNetworkTime();

        NetworkTime.Text = dateTime.ToString(CultureInfo.CurrentCulture);
        SetSystemTime(dateTime);
    }

    private static async Task<DateTime> GetNetworkTime()
    {
        var async = await SharedClient.GetAsync("");
        if (async.Headers.Date == null) throw new Exception("网络时间获取失败");
        var dateTime = async.Headers.Date.GetValueOrDefault().DateTime.ToLocalTime();
        return dateTime;
    }
}