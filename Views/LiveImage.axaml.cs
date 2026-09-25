using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Input;
using System.Threading;

namespace Borealis.Views;

public class LiveImage : TemplatedControl
{
    private WriteableBitmap? _image = null;
    private int _width, _height;
    private Avalonia.Threading.Dispatcher _dispatcher;
    private bool _renderPaused = true;
    private Lock _readLock; // locks bytes while copying
    public byte[]? _inputBytes = null;

    // #region Events

        // Click Event

        // 1. Register the RoutedEvent identifier
        public static readonly RoutedEvent<LiveImageClickArgs> ClickEvent =
            RoutedEvent.Register<LiveImage, LiveImageClickArgs>(
                nameof(Click), 
                RoutingStrategies.Bubble
            );

        // 2. Expose a CLR event wrapper for convenience (and XAML compatibility)
        public event EventHandler<LiveImageClickArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        // 3. Provide a method to raise the event
        protected virtual void OnClick(Point p, MouseButton button)
        {
            LiveImageClickArgs args = new LiveImageClickArgs(ClickEvent, (int)p.X, (int)p.Y, button);
            RaiseEvent(args); // Dispatches the event into Avalonia's event system
        }

    // end Click Event

    // #endregion // Events

    public LiveImage()
    {
        //InitializeComponent();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        // if (e.InitialPressMouseButton == MouseButton.Left)
        // {
            // Mark the event as handled if needed
            e.Handled = true;
            
            var point = e.GetCurrentPoint(this).Position;

            // Trigger your click logic or custom routed event here
            OnClick(point, e.InitialPressMouseButton);
        // }
    }

    public void LoadImage(int width, int height, byte[] bytes, Lock readLock)
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
        _readLock = readLock;

        Render();
    }

    public override void Render(DrawingContext context)
    {
        // if (_renderPaused) return;

        if (_inputBytes is null || _image is null) return;

        var destRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var sourceRect = new Rect(0, 0, _image.Size.Width, _image.Size.Height);
        
        context.DrawImage(_image, sourceRect, destRect);

        // _renderPaused = true;
    }

    public void Render()
    {
        // _readLock.Enter();
        using(var outputBuffer = _image.Lock())
        {
            int stride = outputBuffer.RowBytes;

            for(int row = 0; row < outputBuffer.Size.Height; row++)
            {
                Marshal.Copy(_inputBytes, 4 * row * _width, outputBuffer.Address + row * stride, 4 * _width);
            }
        }
        // _readLock.Exit();

        // _renderPaused = false;
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);

    }
}