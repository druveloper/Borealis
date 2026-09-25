using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using avaTest.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avaTest.Views;

public partial class MainWindow : Window
{
    private MainViewModel vm;
    private byte[] Bytes = new byte[0];
    
    private WriteableBitmap MainBitmap1;
    private WriteableBitmap MainBitmap2;
    private int MapNumber = 1;
    private DateTime StartTime = DateTime.Now;
    private System.Threading.Timer? Timer = null;
    private bool IsClosed = false;

    // private static Image StaticImage;
    // private static TextBlock StaticTextBlock;

    public MainWindow()
    {
        InitializeComponent();

        MainBitmap1 = new WriteableBitmap(
            new Avalonia.PixelSize(400, 400),
            new Avalonia.Vector(300, 300),
            PixelFormat.Bgra8888, AlphaFormat.Opaque);

        MainBitmap2 = new WriteableBitmap(
            new Avalonia.PixelSize(400, 400),
            new Avalonia.Vector(300, 300),
            PixelFormat.Bgra8888, AlphaFormat.Opaque);

        InitializeBitmap(MainBitmap1);

        MainImage.Source = MainBitmap1;

        // vm = new MainViewModel();

        // DataContext = vm;
        // vm.MainBitmap = MainBitmap;
        //MainImage.InvalidateVisual();

        if (Design.IsDesignMode)
        {
            return;
        }

        Timer = new System.Threading.Timer(TimerCallback, null, 800, 800);
        // Timer.Interval = TimeSpan.FromMilliseconds(800);
        // Timer.Tick += TimerCallback;
        // Timer.Start();
    }

    private void InitializeBitmap(WriteableBitmap bitmap)
    {
        Bytes = new byte[bitmap.PixelSize.Height * bitmap.PixelSize.Width * 4];
        byte[] bytes = Bytes;

        for (long y = 0; y < bitmap.PixelSize.Height; y++)
        {
            for (long x = 0; x < bitmap.PixelSize.Width; x++)
            {
                long n = 4 * (bitmap.PixelSize.Width * y + x);

                bytes[n] = (byte)(x % 256);
                bytes[n + 1] = (byte)(y % 256);
                bytes[n + 2] = (byte)((x + y) % 256);
                bytes[n + 3] = 255;
            }
        }

        ILockedFramebuffer buffer = bitmap.Lock();

        Marshal.Copy(bytes, 0, buffer.Address, bytes.Length);

        return;
    }

    private void TimerCallback(object? state) //, EventArgs e)
    {
        if (this.IsClosed)
        {
            if (Timer != null)
            {
                //Timer.Stop();
                Timer = null;
            }

            return;
        }

        int t = (int)((DateTime.Now - StartTime).TotalMilliseconds);
        Dispatcher.UIThread.Invoke((Action)(
            () => TickTextBlock.Text = t.ToString()
        ));
        

        byte[] bytes = Bytes;

        for (long y = 0; y < MainBitmap1.PixelSize.Height; y++)
        {
            for (long x = 0; x < MainBitmap1.PixelSize.Width; x++)
            {
                long n = 4 * (MainBitmap1.PixelSize.Width * y + x);

                bytes[n] = (byte)((x + t) % 256);
                bytes[n + 1] = (byte)((y + t) % 256);
                bytes[n + 2] = (byte)((x + y + t) % 256);
                bytes[n + 3] = 255;
            }
        }

        WriteableBitmap bitmap = MainBitmap1; //(MapNumber == 1 ? MainBitmap1 : MainBitmap2);
        //MapNumber = 1 - MapNumber;
        ILockedFramebuffer buffer = bitmap.Lock();
        // StaticTextBlock.Text = buffer.Address.ToString();

        Marshal.Copy(bytes, 0, buffer.Address, bytes.Length);

        // MainImage.Source = bitmap;
        // Dispatcher.UIThread.Invoke((Action)(
        //     () => MainImage.Source = bitmap
        // ));
        Dispatcher.UIThread.Invoke((Action)(
            () => {
                MainImage.IsVisible = false;
                MainImage.IsVisible = true;
            }
        ));
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {


        // StaticTextBlock = TickTextBlock;
        // StaticImage = MainImage;

        
    }

    private void Window_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Timer.Stop();
        // Timer = null;
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        this.IsClosed = true;
    }
}