using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace avaTest.Views;

public partial class MainWindow : Window
{
    public byte[]? Bytes = null;
    private DateTime StartTime = DateTime.Now;
    private Avalonia.Threading.DispatcherTimer? Timer = null;

    public MainWindow()
    {
        InitializeComponent();
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

    private void TimerCallback(object? state, EventArgs e)
    {
        int t = (int)((DateTime.Now - StartTime).TotalMilliseconds);
        TickTextBlock.Text = t.ToString();

        if (Bytes == null) return;

        byte[] bytes = Bytes;

        for (long y = 0; y < 400; y++)
        {
            for (long x = 0; x < 400; x++)
            {
                long n = 4 * (400 * y + x);

                bytes[n + 0] = (byte)((x + t) % 256);
                bytes[n + 1] = (byte)((y + t) % 256);
                bytes[n + 2] = (byte)((x + y + t) % 256);
                bytes[n + 3] = 255;
            }
        }
        
        MainImage.Render();
    }

    // private void Window_Closed(object? sender, EventArgs e)
    // {
        
    // }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // MainBitmap = new WriteableBitmap(
        //     new Avalonia.PixelSize(400, 400),
        //     new Avalonia.Vector(300, 300),
        //     PixelFormat.Bgra8888, AlphaFormat.Opaque);

        InitializeBitmap();

        if (Bytes == null)
        {
            return;
        }

        MainImage = new MyImage(400, 400, Bytes);

        Timer = new Avalonia.Threading.DispatcherTimer();
        Timer.Interval = TimeSpan.FromMilliseconds(800);
        Timer.Tick += TimerCallback;
        //Timer.Start();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (Timer != null)
        {
            Timer.Stop();
            Timer = null;
        }

        base.OnClosing(e);
    }
}