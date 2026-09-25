using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace avaTest.Views;

public class LiveImage : TemplatedControl
{
    private WriteableBitmap? _image = null;
    private int _width, _height;
    private Avalonia.Threading.Dispatcher _dispatcher;
    private IntPtr _outputBytes;
    public byte[]? _inputBytes = null;


    public LiveImage()
    {
        //InitializeComponent();
    }

    public void LoadImage(int width = 400, int height = 400, byte[]? bytes = null)
    {
        if (bytes == null) return;

        _image = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );

        _width = width;
        _height = height;

        _inputBytes = bytes;
    }

    public override void Render(DrawingContext context)
    {
        if (_inputBytes is null || _image is null) return;

        var destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var sourceRect = new Rect(0, 0, _image.Size.Width, _image.Size.Height);
        
        context.DrawImage(_image, sourceRect, destRect);
    }

    public void Render()
    {
        using(var outputBuffer = _image.Lock())
        {
            int stride = outputBuffer.RowBytes;

            for(int row = 0; row < outputBuffer.Size.Height; row++)
            {
                Marshal.Copy(_inputBytes, 4 * row * _width, outputBuffer.Address + row * stride, 4 * _width);
            }
        }

        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);

    }
}