using System;
using System.Data.SqlTypes;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace avaTest;

/// <summary>
/// Draws points and lines in a 1-dimensional array of bytes meant for an image
/// </summary>
public class ByteDrawer
{
    public Lock ByteLock {get {return _ByteLock;}}
    public byte[] Bytes;
    public int Width;
    public int Height;
    public int BytesPerPixel;
    public Format PixelFormat;
    public Color BackgroundColor = Color.Black;

    private Lock _ByteLock = new Lock();
    private int _drawingsInProgress = 0; // allows drawing functions to call other drawing functions without locking
    private int R = 0;
    private int G = 1;
    private int B = 2;
    private int A = 3;
    private delegate byte getAlphaDelegate(long i);
    private getAlphaDelegate getAlpha;
    
    public enum Format
    {
        RGB, RGBA, BGRA
    }

    public ByteDrawer(int width, int height, Format format)
    {
        Width = width;
        Height = height;
        PixelFormat = format;

        switch(PixelFormat)
        {
            case Format.RGB:
            BackgroundColor = Color.Black;
            BytesPerPixel = 3;
            R = 0;
            G = 1;
            B = 2;
            A = 3;

            getAlpha = (long i) => (255);
            break;

            case Format.RGBA:
            BackgroundColor = Color.Transparent;
            BytesPerPixel = 4;
            R = 0;
            G = 1;
            B = 2;
            A = 3;
            getAlpha = (long i) => (Bytes[i + A]);
            break;

            case Format.BGRA:
            BackgroundColor = Color.Transparent;
            BytesPerPixel = 4;
            B = 0;
            G = 1;
            R = 2;
            A = 3;
            getAlpha = (long i) => (Bytes[i + A]);

            break;

            default: break;
        }

        // allocate bytes
        Bytes = new byte[BytesPerPixel * (Width * Height)];
    }

    /// <summary>
    /// Allows many drawing calls without releasing the lock in between calls
    /// </summary>
    /// <param name="drawingOperation"></param>
    public void DoDrawingOperation(Action drawingOperation)
    {
        lockBegin();

        drawingOperation.Invoke();

        lockEnd();
    }

    /// <summary>
    /// Executes a uniform drawing operation for every pixel
    /// </summary>
    /// <param name="drawingOperation"></param>
    public void DrawEveryPixel(Action<int,int> drawingOperation)
    {
        lockBegin();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                drawingOperation(x, y);
            }
        }

        lockEnd();
    }

    public void DrawLine(int pixelX1, int pixelY1, int pixelX2, int pixelY2, Color color)
    {
        lockBegin();

        // TODO: draw line

        lockEnd();
    }

    public long GetPixelIndex(int x, int y)
    {
        // x = Math.Max(0, Math.Min(400 - 1, x)); // ensure x is within range
        // y = Math.Max(0, Math.Min(400 - 1, y)); // ensure y is within range

        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            //throw new IndexOutOfRangeException("Way off!!");
            return -1;
        }

        x = (Width + x) % Width; // ensure x is within range
        y = (Height + y) % Height; // ensure y is within range

        return (BytesPerPixel * (Width * y + x));
    }

    public Color GetPixelColor(int x, int y)
    {
        long i = GetPixelIndex(x, y);

        if (i < 0)
        {
            return BackgroundColor;
        }

        return Color.FromArgb(getAlpha(i), Bytes[i + R], Bytes[i + G], Bytes[i + B]);
    }

    /// <summary>
    /// Blends the given color with the existing pixel color according to the given color's brightness
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    public void OverlayPixel(int x, int y, double r, double g, double b)
    {
        var oldColor = GetPixelColor(x, y);

        // blend with existing data
        double brightness = Math.Max(r, Math.Max(g, b)) / 255.0;
        SetPixelColor(x, y,
            (1 - brightness) * oldColor.R + brightness * r,
            (1 - brightness) * oldColor.G + brightness * g,
            (1 - brightness) * oldColor.B + brightness * b,
            oldColor.A
        );
    }

    public void SetPixelColor(int x, int y, double r, double g, double b, double a = 255)
    {
        lockBegin();

        long i = GetPixelIndex(x, y);

        if (i < 0)
        {
            return;
        }

        Bytes[i + R] = (byte)Math.Max(0, Math.Min(255, r));
        Bytes[i + G] = (byte)Math.Max(0, Math.Min(255, g));
        Bytes[i + B] = (byte)Math.Max(0, Math.Min(255, b));
        Bytes[i + A] = 255;//(byte)Math.Max(0, Math.Min(255, a));

        lockEnd();
    }

    public void SetPixelColor(int x, int y, Color color)
    {
        SetPixelColor(x, y, color.R, color.G, color.B, color.A);
    }

    private void lockBegin()
    {
        if (_drawingsInProgress == 0 && !_ByteLock.IsHeldByCurrentThread)
        {
            _ByteLock.Enter();
        }

        _drawingsInProgress++;
    }

    private void lockEnd()
    {
        _drawingsInProgress--;

        if (_drawingsInProgress == 0 && _ByteLock.IsHeldByCurrentThread)
        {
            _ByteLock.Exit();
        }
    }
}
