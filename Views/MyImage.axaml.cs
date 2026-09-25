using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace avaTest.Views;

public partial class MyImage : Control
{
    private DrawingContext? ctx = null;
    private WriteableBitmap? _image = null;
    private IntPtr _outputBytes;
    public byte[]? _inputBytes = null;

    public MyImage()
    {
        InitializeComponent();
    }

    public MyImage(int width = 400, int height = 400, byte[]? bytes = null)
    {
        InitializeComponent();

        if (bytes == null) return;

        // var newBytes = new byte[bytes.Length];

        // bytes.CopyTo(newBytes);

        // var byteAddr = new IntPtr(BitConverter.ToInt64(newBytes, 0));

        _image = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque
        );

        _inputBytes = bytes;
        
        //_outputBytes = byteAddr;
    }

    // protected override void OnLoaded(RoutedEventArgs e)
    // {
    //     base.OnLoaded(e);
    //     var uri = new Uri("avares://MyApp/Assets/photo.png");
    //     _image = new Bitmap(AssetLoader.Open(uri));
    //     InvalidateVisual();
    // }

    public override void Render(DrawingContext context)
    {
        if (_inputBytes is null || _image is null) return;
        ctx = context;

        var destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var sourceRect = new Rect(0, 0, _image.Size.Width, _image.Size.Height);

        
        //Marshal.Copy(_inputBytes, 0, _outputBytes, _inputBytes.Length);
        
        //context.DrawImage(_image, sourceRect, destRect);

        context.DrawText(new FormattedText("Hello", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 24, Brushes.Blue),
            new Point(0, 0)
        );
        // int height = 400; // _imageBytes.GetLength(0);
        // int width = 400; //_imageBytes.GetLength(1);
        // for(var y = 0; y < 400; y++)
        // {
        //     for(var x = 0; x < 400; x++)
        //     {
        //         long n = 4 * (400 * y + x);
        //         var p = new Point(x, y);
        //         var brush = new SolidColorBrush(Color.FromRgb(_imageBytes[n + 0], _imageBytes[n + 1], _imageBytes[n + 2]));
        //         context.DrawLine(new Pen(brush, 1), p, p);
        //     }
        // }
    }

    public void Render()
    {
        if (ctx != null)
        {
            InvalidateVisual();
            Render(ctx);
        }
    }
}