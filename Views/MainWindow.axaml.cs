using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Borealis.ViewModels;

namespace Borealis.Views;

public partial class MainWindow : Window
{
    private const int ImageWidth = 400;
    private const int ImageHeight = 400;
    private MainViewModel ViewModel;
    private byte[]? Bytes = null;
    private ByteDrawer Drawer;
    private delegate void PointTransform(double x, double y, out double xout, out double yout);
    private PointTransform Transform = Transforms.Inward;
    private DateTime LastTimerRun = DateTime.Now;
    private bool IsTimerStopped
    {
        get { return ViewModel.IsTimerStopped; }
        set { ViewModel.IsTimerStopped = value; }
    }
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

        ViewModel.PropertyChanged += ((object sender, PropertyChangedEventArgs e) =>
        {
            if (e.PropertyName == "IsTimerStopped")
            {
                if (IsTimerStopped)
                {
                    PauseButton.Content = "Play";
                }
                else
                {
                    PauseButton.Content = "Pause";
                }
            }
        });

        // Bytes = new byte[ImageWidth * ImageHeight * 4];
        Drawer = new ByteDrawer(ImageWidth, ImageHeight, ByteDrawer.Format.RGBA);
        Drawer.BackgroundColor = System.Drawing.Color.Black;
        Bytes = Drawer.Bytes;

        clearBitmap();

        //overlayBitmap(_moons, 0);

        MainImage.LoadImage(ImageWidth, ImageWidth, Bytes, Drawer.ByteLock);

        if (Design.IsDesignMode)
        {
            return;
        }

        // initialize Transform menu

        TransformDropDown.ItemsSource = typeof(Borealis.Transforms).GetMethods().Where((m) => m.IsStatic && m.IsPublic).Select((m) => m.Name);

        // start "timer"

        (new Task(backgroundTask)).Start();


        // Drawer.DrawOverlay(-1, 1, System.Drawing.Color.Red);
        // MainImage.Render();

        MathTextBox.TextChanged += MathTextBox_TextInput;
    }

    private void backgroundTask()
    {
        while (true)
        {
            // check TimerSpeed
            int timerSpeed = 0;
            Dispatcher.Invoke(() =>
            {
                timerSpeed = (int)TimerSpeedSlider.Value;
            });


            double t = 0;
            if (!IsTimerStopped)
            {
                t = simulatedElapsedTime(timerSpeed);
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

                MainImage.Render();
                Thread.Sleep(timerSpeedMultiplier(timerSpeed) * 1000 / 32);
            }
            else if (IsVectorFieldVisible)
            {
                vectorFieldCallback(t);
                MainImage.Render();
                Thread.Sleep(1000 / 32);
            }
            else
            {
                MainImage.Render();
                Thread.Sleep(1000 / 32);
            }
        }
    }

    private int timerSpeedMultiplier(int timerSpeed)
    {
        return (1000 - timerSpeed) * 63 / 1000 + 1;
    }

    private double simulatedElapsedTime(int timerSpeed)
    {
        DateTime now = DateTime.Now;
        double diff = (now - LastTimerRun).TotalMilliseconds / timerSpeedMultiplier(timerSpeed) / 1000.0;
        T += diff;
        LastTimerRun = now;
        return T;
    }

    private void showInfo(string info)
    {
        Dispatcher.UIThread.Invoke(() => InfoTextBlock.Text = info);
    }

    private void clearBitmap()
    {
        Drawer.DrawEveryPixel((ByteDrawer d, int x, int y) =>
        {
            d.SetPixelColor(x, y, 0, 0, 0);
        });
    }

    private void overlayBitmap(Point[] moons, double t)
    {
        Drawer.DoDrawingOperation((ByteDrawer d) =>
        {
            drawMoon(d, moons[0], Color.FromRgb(255, 0, 0));
            drawMoon(d, moons[1], Color.FromRgb(0, 255, 0));
            drawMoon(d, moons[2], Color.FromRgb(0, 0, 255));
        });

        return;
    }

    private void drawMoon(ByteDrawer din, Point moon, Color color)
    {
        din.DrawOverlay(moon.X, moon.Y, System.Drawing.Color.FromArgb(255, color.R, color.G, color.B));
    }

    private void timerCallback(double t) //object? state) //, EventArgs? e = null)
    {
        /******** Normal Renderiing ********/

        // warp frame using vector function

        // double n = t % 2000;
        // double k = Math.PI / 200.0;


        Drawer.DrawTransform();


        // redraw new bitmap

        Moons[0] = rotatePoint(Moons[0], .05);
        Moons[1] = rotatePoint(Moons[1], .0333);
        Moons[2] = rotatePoint(Moons[2], .0166);

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

        Drawer.DrawLine(mouseX, mouseY, pixel2.X, pixel2.Y, System.Drawing.Color.Black, System.Drawing.Color.White);
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
            LastTimerRun = DateTime.Now;
            IsTimerStopped = false;
        }
        else
        {
            IsTimerStopped = true;
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

        Drawer.DoDrawingOperation((ByteDrawer d) =>
        {
            d.DrawLine(x1, y1, x2, y2, color);
            d.DrawLine(x1, y1 + 1, x2, y2 + 1, color);
        });
    }

    private void drawVectorField()
    {
        double px, py, pixelWidth = Coord.GraphSize.X / Coord.ImageWidth;
        Drawer.DrawEveryPixel((ByteDrawer d, int x, int y) =>
        {
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
            drawLine((int)p.X, (int)p.Y, (int)p.X + 1, (int)p.Y, 2);
        }
        else
        {
            drawLine((int)CursorPosition?.X, (int)CursorPosition?.Y, (int)p.X + 1, (int)p.Y, 2);
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
        drawLine((int)p.X, (int)p.Y, (int)p.X + 1, (int)p.Y + 1, 2);
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

    private void TransformDropDown_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        string transformName = ((ComboBox)sender).SelectedItem?.ToString() ?? "";

        Transform = typeof(Borealis.Transforms).GetMethod(transformName)?.CreateDelegate<PointTransform>() ?? Transforms.None;
    }

    private void MathTextBox_TextInput(object? sender, TextChangedEventArgs e)
    {
        try
        {
            MathTextResponse.Text = MathValidator.ValidateMathExpression(MathTextBox.Text ?? "");
        }
        catch (ApplicationException ex)
        {
            MathTextResponse.Text = ex.Message;
        }
    }

    private void MathTextResponse_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var match = new System.Text.RegularExpressions.Regex(" position (\\d+)").Match(MathTextResponse.Text);

        if (match.Success)
        {
            int position = Int32.Parse(match.Groups[1].Value);
            MathTextBox.CaretIndex = position;
        }
    }
}