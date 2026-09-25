using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus.Protocol;

namespace avaTest.Views;

public partial class MainWindow : Window
{
    public byte[]? Bytes = null;
    private DateTime StartTime = DateTime.Now;
    private Avalonia.Threading.DispatcherTimer? Timer = null;
    private System.Threading.Timer? Timer2 = null;

    public MainWindow()
    {
        InitializeComponent();

        InitializeBitmap();

        MainImage.LoadImage(400, 400, Bytes);

        if (Design.IsDesignMode)
        {
            return;
        }

        Timer2 = new Timer((n => {
            int t = (int)((DateTime.Now - StartTime).TotalMilliseconds);
            Dispatcher.UIThread.Invoke(() => TimeTextBlock.Text = t.ToString());
            TimerCallback(t);
            MainImage.Render();
            // Dispatcher.UIThread.Invoke(() => MainImage.Render());
        }), null, 800, 80);

        // Timer = new Avalonia.Threading.DispatcherTimer();
        // Timer.Interval = TimeSpan.FromMilliseconds(800);
        // Timer.Tick += TimerCallback;
        //Timer.Start();
    }

    private void InitializeBitmap()
    {
        Bytes = new byte[400 * 400 * 4];
        byte[] bytes = Bytes;

        for (long y = 0; y < 400; y++)
        {
            for (long x = 0; x < 400; x++)
            {
                long n = 4 * (400 * y + x);

                bytes[n]     = (byte)(x % 256);
                bytes[n + 1] = (byte)(y % 256);
                bytes[n + 2] = (byte)((x + y) % 256);
                bytes[n + 3] = 255;
            }
        }

        return;
    }

    private void TimerCallback(int t) //object? state) //, EventArgs? e = null)
    {
        if (Bytes == null) return;

        byte[] bytes = Bytes;

        // for (long y = 0; y < 400; y++)
        // {
        //     for (long x = 0; x < 400; x++)
        //     {
        //         long n = 4 * (400 * y + x);

        //         bytes[n + 0] = (byte)((x + t) % 256);
        //         bytes[n + 1] = (byte)((y + t) % 256);
        //         bytes[n + 2] = (byte)((x + y + t) % 256);
        //         bytes[n + 3] = 255;
        //     }
        // }
    }
}