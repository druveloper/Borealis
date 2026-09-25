using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Borealis.ViewModels;
using Tmds.DBus.Protocol;

namespace Borealis.Views;

public partial class MainWindow : Window
{
    private const int ImageWidth = 400;
    private const int ImageHeight = 400;
    private MainViewModel ViewModel;
    private byte[]? Bytes = null;
    private ByteDrawer Drawer;
    private delegate void PointTransform(double x, double y, out double xout, out double yout);
    private PointTransform Transform = Transforms.Shrink;
    private DateTime StartTime = DateTime.Now;
    private DateTime LastTimerRun = DateTime.Now;
    private System.Threading.Timer? Timer = null;
    private bool IsTimerStopped = false;
    private double T = 0;
    private CoordinateConverter Coord = new CoordinateConverter(400, 400, new Point(2, 2), new Point(0, 0));
    private Point[] Moons = [new Point(-0.170, 0.281), new Point(0.58, 0.66), new Point(0.338, 0.61)];
    // private Point[] _points = [new Point(170, 281), new Point(88, 96), new Point(338, 81)];
    private bool IsVectorFieldVisible = false;
    private Point? CursorPosition = null; // cursor position when over the graph image

    public MainWindow()
    {
        InitializeComponent();

        ViewModel = new MainViewModel();
        DataContext = ViewModel;

        // Bytes = new byte[ImageWidth * ImageHeight * 4];
        Drawer = new ByteDrawer(ImageWidth, ImageHeight, ByteDrawer.Format.BGRA);
        Drawer.BackgroundColor = System.Drawing.Color.Black;
        Bytes = Drawer.Bytes;

        clearBitmap();

        //overlayBitmap(_moons, 0);

        MainImage.LoadImage(ImageWidth, ImageWidth, Bytes, Drawer.ByteLock);

        if (Design.IsDesignMode)
        {
            return;
        }

        // MainImage.Render();

        //Timer = new Timer((n =>
        (new Task(() =>
        {
            while(true)
            {
            // check TimerSpeed
            int timerSpeed = 0;
            Dispatcher.Invoke(() =>
            {
                timerSpeed = (int)TimerSpeedSlider.Value;
            });
            if (timerSpeed < 1000)
            {
                if ((DateTime.Now - LastTimerRun).TotalMilliseconds < 2100 - 2 * timerSpeed)
                {
                    return;
                }

                LastTimerRun = DateTime.Now;
            }


            double t = 0;
            if (!IsTimerStopped)
            {
                t = T + simulatedElapsedTime();
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
            else if (IsVectorFieldVisible)
            {
                vectorFieldCallback(t);
            }

            MainImage.Render();
            // Dispatcher.UIThread.Invoke(() => MainImage.Render());

            Thread.Sleep(1000 / 32);
            }

        //}), null, 800, 1000 / 10);
        })).Start();
    }

    private double simulatedElapsedTime()
    {
        return (DateTime.Now - StartTime).TotalMilliseconds * 100 / (2100 - 2 * ViewModel.TimerSpeed) / 1000.0;
    }

    private void showInfo(string info)
    {
        Dispatcher.UIThread.Invoke(() => InfoTextBlock.Text = info);
    }

    private void clearBitmap()
    {
        Drawer.DrawEveryPixel((ByteDrawer d, int x, int y) => {
            d.SetPixelColor(x, y, 0, 0, 0);
        });
    }

    private void overlayBitmap(Point[] moons, double t)
    {
        Drawer.DoDrawingOperation((ByteDrawer d) => {
            drawMoon(d, moons[0], Color.FromRgb(255, 0, 0));
            drawMoon(d, moons[1], Color.FromRgb(0, 255, 0));
            drawMoon(d, moons[2], Color.FromRgb(0, 0, 255));
        });

        return;
    }

    private void drawMoon(ByteDrawer din, Point moon, Color color)
    {
        Point point;
        double x, y, dist;
        din.DrawEveryPixel((ByteDrawer d, int ix, int iy) => {
            point = Coord.PixelToPoint(ix, iy);
            x = point.X;
            y = point.Y;
            dist = Math.Max(0, Math.Min(0.50, Math.Abs(x - moon.X) + Math.Abs(y - moon.Y)));

            d.OverlayPixel(ix, iy,
                color.R * (1 - Math.Pow(dist / 0.50, 1)),
                color.G * (1 - Math.Pow(dist / 0.50, 1)),
                color.B * (1 - Math.Pow(dist / 0.50, 1))
            );
        });
    }

    private void timerCallback(double t) //object? state) //, EventArgs? e = null)
    {
        /******** Normal Renderiing ********/

        // warp frame using vector function

        // double n = t % 2000;
        // double k = Math.PI / 200.0;
        double xt, yt;
        Drawer.DrawEveryPixel((ByteDrawer d, int x, int y) => {
            var point1 = Coord.PixelToPoint(x, y);

            
            Transform(point1.X, point1.Y, out xt, out yt);

            var p2 = getPoint(xt, yt);

            d.SetPixelColor(x, y, p2.r - 5, p2.g - 5, p2.b - 5, 255);

            //setPoint(point1.X, point1.Y, p2.r - 10, p2.g - 10, p2.b - 10, 255);
        });

        // MainImage.Render();
        // Thread.Sleep(80 * 15);

        // redraw new bitmap

        Moons[0] = rotatePoint(Moons[0], .15);
        Moons[1] = rotatePoint(Moons[1], .10);
        Moons[2] = rotatePoint(Moons[2], .05);

        overlayBitmap(Moons, 0);
    }

    private void vectorFieldCallback(double t)
    {
        /******** Vector Field Renderiing ********/


        // display Vector Field and the vector at cursor position

        drawVectorField();

        if (CursorPosition is null)
        {
            return;
        }

        var mouseX = (int)CursorPosition.Value.X;
        var mouseY = (int)CursorPosition.Value.Y;
        double px, py;

        var point1 = Coord.PixelToPoint(mouseX, mouseY);
        Transform(point1.X, point1.Y, out px, out py);

        var pixel2 = Coord.PointToPixel(px, py);

        Drawer.DoDrawingOperation((d) =>
        {
            d.DrawLine(mouseX, mouseY, pixel2.X, pixel2.Y, System.Drawing.Color.Black, System.Drawing.Color.White);
            d.DrawLine(mouseX, mouseY, pixel2.X, pixel2.Y, System.Drawing.Color.Black, System.Drawing.Color.White);
            d.DrawLine(mouseX, mouseY, pixel2.X, pixel2.Y, System.Drawing.Color.Black, System.Drawing.Color.White);
        });

        // Drawer.DrawLine(,
        //     DateTime.Now.Millisecond % 500 < 250 ? System.Drawing.Color.Black : System.Drawing.Color.White
        // );
    }

    private GraphPoint getPoint(double x, double y)
    {
        var p = Coord.PointToPixel(x, y);
        
        return new GraphPoint(new Point(x, y), Drawer.GetPixelColor(p.X, p.Y));
    }

    private void setPoint(double x, double y, double r, double g, double b, double a = 255)
    {
        var p = Coord.PointToPixel(x, y);
        
        Drawer.SetPixelColor(p.X, p.Y, r, g, b, a);
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

    private void displayPointInfo(double x, double y)
    {
        var point = getPoint(x, y);

        xTextBox.Text = point.x.ToString("F4");
        yTextBox.Text = point.y.ToString("F4");
        rTextBlock.Text = ((int)point.r).ToString();
        gTextBlock.Text = ((int)point.g).ToString();
        bTextBlock.Text = ((int)point.b).ToString();
        aTextBlock.Text = ((int)point.a).ToString();

        double x2, y2;
        Transform(x, y, out x2, out y2);
        fxTextBlock.Text = x2.ToString("F4");
        fyTextBlock.Text = y2.ToString("F4");
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
            T += simulatedElapsedTime();
        }
    }

    private void GetPixelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

    }

    private void MainImage_Click(object? sender, Borealis.LiveImageClickArgs e)
    {
        if (e.MouseButton == Avalonia.Input.MouseButton.Right)
        {
            var p = Coord.PixelToPoint((int)e.MousePoint.X, (int)e.MousePoint.Y);

            displayPointInfo(p.X, p.Y);
            return;
        }
    }

    private void drawLine(int x1, int y1, int x2, int y2, int size)
    {
        if (x1 < 0 || x1 > ImageWidth || y1 < 0 || y1 > ImageWidth ||
            x2 < 0 || x2 > ImageWidth || y2 < 0 || y2 > ImageWidth ||
            size <= 0)
        {
            return;
        }

        System.Drawing.Color color = System.Drawing.Color.White;

        Drawer.DoDrawingOperation((ByteDrawer d) => {
            d.DrawLine(x1, y1, x2, y2, color);
            d.DrawLine(x1, y1+1, x2, y2+1, color);
        });
    }

    private void drawVectorField()
    {
        double px, py, pixelWidth = Coord.GraphSize.X / Coord.ImageWidth;
        Drawer.DrawEveryPixel((ByteDrawer d, int x, int y) => {
            var point1 = Coord.PixelToPoint(x, y);

            Transform(point1.X, point1.Y, out px, out py);

            var point2 = new Point(px, py);
            
            double dist = Avalonia.Point.Distance(point1, point2) / pixelWidth / 10.0;
            d.SetPixelColor(x, y, 255 * (dist - 1), 255 * dist /* (dist < 1 ? 1:0)*/, 255 * (1 - dist), 255);
        });
    }

    private void MainImage_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (IsVectorFieldVisible)
        {
            CursorPosition = e.GetCurrentPoint(MainImage).Position;
            return;
        }

        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var p = e.GetCurrentPoint(MainImage).Position;

        if (CursorPosition is null)
        {
            drawLine((int)p.X, (int)p.Y, (int)p.X+1, (int)p.Y, 2);
        }
        else
        {
            drawLine((int)CursorPosition?.X, (int)CursorPosition?.Y, (int)p.X+1, (int)p.Y, 2);
        }

        CursorPosition = p;
    }

    private void MainImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var p = e.GetCurrentPoint(MainImage).Position;
        drawLine((int)p.X, (int)p.Y, (int)p.X+1, (int)p.Y+1, 2);
    }

    private void MainImage_PointerExited(object? sender, PointerEventArgs e)
    {
        CursorPosition = null;
    }

    private void OverlayButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        overlayBitmap(Moons, T);
    }

    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        clearBitmap();
    }

    private void VectorButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!IsVectorFieldVisible)
        {
            IsTimerStopped = true;
            PauseButton.IsEnabled = false;
            OverlayButton.IsEnabled = false;

            VectorButton.Content = "Live Iamge";
            MainImage.Cursor = new Cursor(StandardCursorType.None);
        }
        else
        {
            clearBitmap();
            overlayBitmap(Moons, T);

            PauseButton.IsEnabled = true;
            OverlayButton.IsEnabled = true;

            VectorButton.Content = "Vector Field";
            MainImage.Cursor = new Cursor(StandardCursorType.Cross);
        }

        IsVectorFieldVisible = !IsVectorFieldVisible;
    }
}