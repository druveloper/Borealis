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
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Threading;
using avaTest.ViewModels;
using Tmds.DBus.Protocol;

namespace avaTest.Views;

public partial class MainWindow : Window
{
    private const int ImageWidth = 400;
    private const int ImageHeight = 400;
    private MainViewModel ViewModel;
    private byte[]? Bytes = null;
    private ByteDrawer Drawer;
    private delegate Point PointTransform(double x, double y);
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

        MainImage.LoadImage(ImageWidth, ImageWidth, Bytes);

        if (Design.IsDesignMode)
        {
            return;
        }

        // MainImage.Render();

        Timer = new Timer((n =>
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
                vectorFieldCallback();
            }

            MainImage.Render();
            // Dispatcher.UIThread.Invoke(() => MainImage.Render());

        }), null, 800, 100);
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
        Drawer.DrawEveryPixel((int x, int y) => {
            Drawer.SetPixelColor(x, y, 0, 0, 0);
        });
    }

    private void overlayBitmap(Point[] moons, double t)
    {
        Drawer.DrawEveryPixel((int ix, int iy) => {

            var point = Coord.PixelToPoint(ix, iy);
            double x = point.X;
            double y = point.Y;

            var dist1 = Math.Max(0, Math.Min(0.50, Math.Abs(x - moons[0].X) + Math.Abs(y - moons[0].Y)));
            var dist2 = Math.Max(0, Math.Min(0.50, Math.Abs(x - moons[1].X) + Math.Abs(y - moons[1].Y)));
            var dist3 = Math.Max(0, Math.Min(0.50, Math.Abs(x - moons[2].X) + Math.Abs(y - moons[2].Y)));

            Drawer.OverlayPixel(ix, iy,
                255 - 255 * Math.Pow(dist1 / 0.50, 1),
                255 - 255 * Math.Pow(dist2 / 0.50, 1),
                255 - 255 * Math.Pow(dist3 / 0.50, 1)
            );
        });

        return;
    }

    private void timerCallback(double t) //object? state) //, EventArgs? e = null)
    {
        /******** Normal Renderiing ********/

        // warp frame using vector function

        // double n = t % 2000;
        // double k = Math.PI / 200.0;
        Drawer.DrawEveryPixel((int x, int y) => {
            var point1 = Coord.PixelToPoint(x, y);

            var point2 = Transform(point1.X, point1.Y);

            var p2 = getPoint(point2.X, point2.Y);

            setPoint(point1.X, point1.Y, p2.r - 10, p2.g - 10, p2.b - 10, 255);
        });

        // MainImage.Render();
        // Thread.Sleep(80 * 15);

        // redraw new bitmap

        Moons[0] = rotatePoint(Moons[0], .15);
        Moons[1] = rotatePoint(Moons[1], .10);
        Moons[2] = rotatePoint(Moons[2], .05);

        overlayBitmap(Moons, 0);
    }

    private void vectorFieldCallback()
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

        var point1 = Coord.PixelToPoint(mouseX, mouseY);
        var point2 = Transform(point1.X, point1.Y);

        var pixel2 = Coord.PointToPixel(point2);

        Drawer.DrawLine(mouseX, mouseY, pixel2.X, pixel2.Y, System.Drawing.Color.White);
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

        var point2 = Transform(x, y);
        fxTextBlock.Text = point2.X.ToString("F4");
        fyTextBlock.Text = point2.Y.ToString("F4");
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

    private void MainImage_Click(object? sender, avaTest.LiveImageClickArgs e)
    {
        if (e.MouseButton == Avalonia.Input.MouseButton.Right)
        {
            var p = Coord.PixelToPoint((int)e.MousePoint.X, (int)e.MousePoint.Y);

            displayPointInfo(p.X, p.Y);
            return;
        }
    }

    private void drawDot(int px, int py, int size)
    {
        if (px < 0 || px > ImageWidth || py < 0 || py > ImageWidth)
        {
            return;
        }

        System.Drawing.Color color = System.Drawing.Color.White;

        Drawer.DoDrawingOperation(() => {
            Drawer.SetPixelColor(px, py, color);
            Drawer.SetPixelColor(px+1, py, color);
            Drawer.SetPixelColor(px, py+1, color);
            Drawer.SetPixelColor(px+1, py+1, color);
        });
    }

    private void drawVectorField()
    {
        double pixelWidth = Coord.GraphSize.X / Coord.ImageWidth;
        Drawer.DrawEveryPixel((int x, int y) => {
            var point1 = Coord.PixelToPoint(x, y);

            var point2 = Transform(point1.X, point1.Y);

            var p2 = getPoint(point2.X, point2.Y);

            double dist = Avalonia.Point.Distance(point1, point2) / pixelWidth / 10.0;
            Drawer.SetPixelColor(x, y, 255 * (dist - 1), 255 * dist /* (dist < 1 ? 1:0)*/, 255 * (1 - dist), 255);
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
        drawDot((int)p.X, (int)p.Y, 2);
    }

    private void MainImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var p = e.GetCurrentPoint(MainImage).Position;
        drawDot((int)p.X, (int)p.Y, 2);
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
        }
        else
        {
            clearBitmap();
            overlayBitmap(Moons, T);

            PauseButton.IsEnabled = true;
            OverlayButton.IsEnabled = true;

            MainImage.Render();
            VectorButton.Content = "Vector Field";
        }

        IsVectorFieldVisible = !IsVectorFieldVisible;
    }
}