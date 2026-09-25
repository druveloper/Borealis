using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus.Protocol;

namespace avaTest.Views;

public partial class MainWindow : Window
{
    public byte[]? Bytes = null;
    private delegate Point PointTransform(double x, double y);
    private PointTransform Transform = Transforms.Custom;
    private DateTime StartTime = DateTime.Now;
    private System.Threading.Timer? Timer = null;
    private bool IsTimerStopped = false;
    private double T = 0;
    private CoordinateConverter Coord = new CoordinateConverter(400, 400, new Point(2, 2), new Point(0, 0));
    private Point[] _points = [new Point(-0.170, 0.281), new Point(0.58, 0.66), new Point(0.338, 0.61)];
    // private Point[] _points = [new Point(170, 281), new Point(88, 96), new Point(338, 81)];

    public MainWindow()
    {
        InitializeComponent();

        Bytes = new byte[400 * 400 * 4];

        clearBitmap();
        
        drawBitmap(_points, 0);

        MainImage.LoadImage(400, 400, Bytes);

        if (Design.IsDesignMode)
        {
            return;
        }


        Timer = new Timer((n =>
        {
            // return if UI is done
            if (!Dispatcher.UIThread.Thread.IsAlive)
            {
                return;
            }

            double t = 0;
            if (!IsTimerStopped)
            {
                t = T + (DateTime.Now - StartTime).TotalMilliseconds / 1000.0;
            }
            else
            {
                t = T;
            }

            Dispatcher.UIThread.Invoke(() => TimeTextBlock.Text = t.ToString());

            if (!IsTimerStopped)
            {
                try
                {
                    timerCallback(t);
                }
                catch (Exception e)
                {
                    showInfo(e.Message);
                }
            }

            MainImage.Render();
            // Dispatcher.UIThread.Invoke(() => MainImage.Render());
        }), null, 800, 80);

        // Timer = new Avalonia.Threading.DispatcherTimer();
        // Timer.Interval = TimeSpan.FromMilliseconds(800);
        // Timer.Tick += TimerCallback;
        //Timer.Start();
    }

    private void showInfo(string info)
    {
        Dispatcher.UIThread.Invoke(() => InfoTextBlock.Text = info);
    }

    private void clearBitmap()
    {
        for (double x = -200; x < 200; x++)
        {
            for (double y = -200; y < 200; y++)
            {
                setPoint(x / 200.0, y / 200.0, 0, 0, 0, 255);
            }
        }
    }

    private void drawBitmap(Point[] points, double t)
    {
        // for (int x = 0; x < 400; x++)
        // {
        //     int y = (int)(200 + 50 * Math.Cos((x - 200) / 20.0 + t));
        //     setPixel(x, y, 255, 0, 0);
        // }

        // var n = ((t * 20) + 20) % 20;

        // double px, py;
        // double r, g, b;
        // double pixelWidth = 2.0 / 400;

        // for (double x = 0; x < 400; x += 20)
        // {
        //     for (double y = 0; y < 400; y += 20)
        //     {
        //         var point = Coord.PixelToPoint((int)(x+n), (int)(y+n));
        //         px = point.X;
        //         py = point.Y;
        //         r = 255 * (x / 400.0);
        //         g = 255 * (y / 400.0);
        //         b = 255 * ((x + y) / 400.0);

        //         setPoint(px + 0, py + 0, r, g, b);
        //         setPoint(px + pixelWidth, py + 0, r, g, b);
        //         setPoint(px + 0, py + pixelWidth, r, g, b);
        //         setPoint(px + pixelWidth, py + pixelWidth, r, g, b);
        //     }
        // }


        double x, y;
        for (double iy = -200; iy < 200; iy++)
        {
            for (double ix = -200; ix < 200; ix++)
            {
                x = ix / 200;
                y = iy / 200;
                var dist1 = Math.Max(0, Math.Min(0.50, Math.Abs(x - points[0].X) + Math.Abs(y - points[0].Y)));
                var dist2 = Math.Max(0, Math.Min(0.50, Math.Abs(x - points[1].X) + Math.Abs(y - points[1].Y)));
                var dist3 = Math.Max(0, Math.Min(0.50, Math.Abs(x - points[2].X) + Math.Abs(y - points[2].Y)));

                var oldPoint = getPoint(x, y);
                var newPoint = new Pixel(x, y,
                    (byte)(255 - 255 * Math.Pow(dist1 / 0.50, 1)),
                    (byte)(255 - 255 * Math.Pow(dist2 / 0.50, 1)),
                    (byte)(255 - 255 * Math.Pow(dist3 / 0.50, 1)),
                    255
                );

                // blend with existing data
                setPoint(x, y,
                    .9 * oldPoint.r + .5 * newPoint.r,
                    .9 * oldPoint.g + .5 * newPoint.g,
                    .9 * oldPoint.b + .5 * newPoint.b,
                    255
                );

                // setPixel(x, y,
                //     (byte)(255 * x / 400.0),
                //     (byte)(255 * y / 400.0),
                //     (byte)(255 * (x + y) / 800.0),
                //     255
                // );
            }
        }

        return;
    }

    private void timerCallback(double t) //object? state) //, EventArgs? e = null)
    {
        if (Bytes == null) return;

        //ShowInfo(((int)(2 * Math.Cos(t / 2000.0))).ToString());

        // warp frame using vector function

        double n = t % 2000;
        double k = Math.PI / 200.0;
        for (int y = 0; y < 400; y++)
        {
            for (int x = 0; x < 400; x++)
            {
                var point1 = Coord.PixelToPoint(x, y);

                var point2 = Transform(point1.X, point1.Y);

                var p2 = getPoint(point2.X, point2.Y);


                setPoint(point1.X, point1.Y, p2.r - 10, p2.g - 10, p2.b - 10, 255);
            }
        }

        // redraw new bitmap

        _points[0] = rotatePoint(_points[0], .15);
        _points[1] = rotatePoint(_points[1], .10);
        _points[2] = rotatePoint(_points[2], .05);

        drawBitmap(_points, n);
    }

    private long getPixelIndex(byte[] bytes, int x, int y)
    {
        // x = Math.Max(0, Math.Min(400 - 1, x)); // ensure x is within range
        // y = Math.Max(0, Math.Min(400 - 1, y)); // ensure y is within range

        if (x < 0 || x >= 400 || y < 0 || y >= 400)
        {
            //throw new IndexOutOfRangeException("Way off!!");
            return -1;
        }

        x = (400 + x) % 400; // ensure x is within range
        y = (400 + y) % 400; // ensure y is within range

        return (4 * (400 * y + x));
    }

    private Pixel getPoint(double x, double y)
    {
        var p = Coord.PointToPixel(x, y);
        long i = getPixelIndex(Bytes, p.X, p.Y);

        if (i < 0)
        {
            return new Pixel(0, 0, 0, 0, 0, 255);
        }

        return new Pixel(x, y, Bytes[i + 2], Bytes[i + 1], Bytes[i + 0], Bytes[i + 3]);
    }

    private void setPoint(double x, double y, double r, double g, double b, double a = 255)
    {
        var p = Coord.PointToPixel(x, y);
        long i = getPixelIndex(Bytes, p.X, p.Y);

        if (i < 0)
        {
            return;
        }

        Bytes[i] = (byte)Math.Max(0, Math.Min(255, b));
        Bytes[i + 1] = (byte)Math.Max(0, Math.Min(255, g));
        Bytes[i + 2] = (byte)Math.Max(0, Math.Min(255, r));
        Bytes[i + 3] = (byte)Math.Max(0, Math.Min(255, a));
    }

    private Point rotatePoint(Point p, double angleDelta, Point? center = null)
    {
        Point c;

        if (center is null)
        {
            c = new Point(0, 0);
        }
        else
        {
            c = center ?? p;
        }

        var dist = Point.Distance(p, c);
        var angle = Math.Atan2(p.Y - c.Y, p.X - c.X);

        // shift point to center
        var s = new Point(p.X - c.X, p.Y - c.Y);

        // rotate by angleDelta
        s = new Point(
            dist * Math.Cos(angle + angleDelta),
            dist * Math.Sin(angle + angleDelta)
        );

        // shift back relative to center
        return new Point(s.X + c.X, s.Y + c.Y);
    }

    private void displayPixelInfo(double x, double y)
    {
        var pixel = getPoint(x, y);

        xTextBox.Text = pixel.x.ToString();
        yTextBox.Text = pixel.y.ToString();
        rTextBlock.Text = ((int)pixel.r).ToString();
        gTextBlock.Text = ((int)pixel.g).ToString();
        bTextBlock.Text = ((int)pixel.b).ToString();

        var point = Transform(x, y);
        fxTextBlock.Text = point.X.ToString();
        fyTextBlock.Text = point.Y.ToString();
    }

    private void PauseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IsTimerStopped)
        {
            StartTime = DateTime.Now;
            IsTimerStopped = false;
        }
        else
        {
            IsTimerStopped = true;
            T += (DateTime.Now - StartTime).TotalMilliseconds / 1000.0;
        }
    }

    private void GetPixelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

    }

    private void MainImage_Click(object? sender, avaTest.LiveImageClickArgs e)
    {
        if (e.MouseButton == Avalonia.Input.MouseButton.Right)
        {
            var p = Coord.PixelToPoint((int)e.MousePoint.X, (int)e.MousePoint.Y);

            displayPixelInfo(p.X, p.Y);
            return;
        }
    }

    private void drawPoint(int px, int py, int size)
    {
        double pixelWidth = 2.0 / 400;
        var p = Coord.PixelToPoint(px, py);

        if (p.X < -1 || p.X > 1 || p.Y < -1 || p.Y > 1)
        {
            return;
        }

        byte r = 255, g = 255, b = 255;

        setPoint(p.X + 0         , p.Y + 0         , r, g, b);
        setPoint(p.X + pixelWidth, p.Y + 0         , r, g, b);
        setPoint(p.X + 0         , p.Y + pixelWidth, r, g, b);
        setPoint(p.X + pixelWidth, p.Y + pixelWidth, r, g, b);
    }

    private void MainImage_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var p = e.GetCurrentPoint(MainImage).Position;
        drawPoint((int)p.X, (int)p.Y, 2);
    }

    private void MainImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var p = e.GetCurrentPoint(MainImage).Position;
        drawPoint((int)p.X, (int)p.Y, 2);
    }
}