using System;
using System.Data.SqlTypes;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;

namespace avaTest;

/// <summary>
/// Draws points and lines in a 1-dimensional array of bytes meant for an image
/// </summary>
public class ByteDrawer
{
    public Lock ByteLock {get {return _ByteLock;}}
    public byte[] Bytes {get {return _BytePointer;}}
    public int Width;
    public int Height;
    public int BytesPerPixel;
    public Format PixelFormat;
    public Color BackgroundColor = Color.Black;

    private byte[] _MainBytes;
    private byte[] _AuxiliaryBytes; // saves output of draw operations before updating main bytes
    private byte[] _BytePointer; // points to either main bytes or auxiliary bytes for drawing
    private Lock _ByteLock = new Lock();
    private int _DrawingsInProgress = 0; // allows drawing functions to call other drawing functions without locking
    private int _R = 0;
    private int _G = 1;
    private int _B = 2;
    private int _A = 3;
    private delegate byte getAlphaDelegate(long i);
    private getAlphaDelegate _GetAlpha;
    
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
            _R = 0;
            _G = 1;
            _B = 2;
            _A = 3;

            _GetAlpha = (long i) => (255);
            break;

            case Format.RGBA:
            BackgroundColor = Color.Transparent;
            BytesPerPixel = 4;
            _R = 0;
            _G = 1;
            _B = 2;
            _A = 3;
            _GetAlpha = (long i) => (_MainBytes[i + _A]);
            break;

            case Format.BGRA:
            BackgroundColor = Color.Transparent;
            BytesPerPixel = 4;
            _B = 0;
            _G = 1;
            _R = 2;
            _A = 3;
            _GetAlpha = (long i) => (_MainBytes[i + _A]);

            break;

            default: break;
        }

        // allocate bytes
        _MainBytes = new byte[BytesPerPixel * (Width * Height)];
        _AuxiliaryBytes = new byte[_MainBytes.Length];
        _BytePointer = _MainBytes;
    }

    /// <summary>
    /// Allows many drawing calls without releasing the lock in between calls
    /// </summary>
    /// <param name="drawingOperation"></param>
    public void DoDrawingOperation(Action<ByteDrawer> drawingOperation)
    {
        drawBegin();

        drawingOperation(this);

        drawEnd();
    }

    /// <summary>
    /// Executes a uniform drawing operation for every pixel
    /// </summary>
    /// <param name="drawingOperation"></param>
    public void DrawEveryPixel(Action<ByteDrawer, int,int> drawingOperation)
    {
        drawBegin(); //_ByteLock.Enter();

        _MainBytes.CopyTo(_AuxiliaryBytes, 0);
        _BytePointer = _AuxiliaryBytes;

        //Parallel.For(0, Height, (int y) =>
        for(int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                drawingOperation(this, x, y);
            }
        }//);
        
        _AuxiliaryBytes.CopyTo(_MainBytes, 0);
        _BytePointer = _MainBytes;

        drawEnd(); //_ByteLock.Exit();
    }

    /// <summary>
    /// Draws a continuous line between two pixels
    /// </summary>
    /// <param name="pixelX1"></param>
    /// <param name="pixelY1"></param>
    /// <param name="pixelX2"></param>
    /// <param name="pixelY2"></param>
    /// <param name="color"></param>
    /// <param name="outline"></param>
    public void DrawLine(int pixelX1, int pixelY1, int pixelX2, int pixelY2, Color color, Color? outline = null)
    {
        drawBegin();
        
        ref int minX = ref (pixelX1 < pixelX2) ? ref pixelX1 : ref pixelX2;
        ref int maxX = ref (pixelX1 < pixelX2) ? ref pixelX2 : ref pixelX1;
        ref int minY = ref (pixelY1 < pixelY2) ? ref pixelY1 : ref pixelY2;
        ref int maxY = ref (pixelY1 < pixelY2) ? ref pixelY2 : ref pixelY1;

        // determine whether to use x or y as independent variable

        if (maxX - minX >= maxY - minY)
        {
            double slope = (double)(pixelY2 - pixelY1) / (pixelX2 - pixelX1);
            double offset = pixelY1 - slope * pixelX1;

            for(int x = minX; x <= maxX; x++)
            {
                int y = (int)(slope * x + offset);
                SetPixelColor(x, y, color);

                if (outline is not null)
                {
                    SetPixelColor(x, y-1, outline ?? Color.White);
                    SetPixelColor(x, y+1, outline ?? Color.White);
                }
            }

            // draw outline cap
            if (outline is not null)
            {
                double slope2 = 1 / slope;
                double offset2 = pixelX2 - (slope2 * pixelY2);

                //SetPixelColor((int)(slope2 * (pixelY2-1) + offset2), pixelY2-1, outline ?? Color.White);
                SetPixelColor((int)(slope2 * pixelY2 + offset2),     pixelY2,   outline ?? Color.White);
                //SetPixelColor((int)(slope2 * (pixelY2+1) + offset2), pixelY2+1, outline ?? Color.White);
            }
        }
        else
        {
            double slope = (double)(pixelX2 - pixelX1) / (pixelY2 - pixelY1);
            double offset = pixelX1 - slope * pixelY1;

            for(int y = minY; y <= maxY; y++)
            {
                int x = (int)(slope * y + offset);
                SetPixelColor(x, y, color);

                if (outline is not null)
                {
                    SetPixelColor(x-1, y, outline ?? Color.White);
                    SetPixelColor(x+1, y, outline ?? Color.White);
                }
            }

            // draw outline cap
            if (outline is not null)
            {
                double slope2 = 1 / slope;
                double offset2 = pixelY2 - (slope2 * pixelX2);

                //SetPixelColor(pixelX2-1, (int)(slope2 * (pixelX2-1) + offset2), outline ?? Color.White);
                SetPixelColor(pixelX2,   (int)(slope2 * pixelX2 + offset2),     outline ?? Color.White);
                //SetPixelColor(pixelX2+1, (int)(slope2 * (pixelX2+1) + offset2), outline ?? Color.White);

            }
        }

        drawEnd();
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

        // x = (Width + x) % Width; // ensure x is within range
        // y = (Height + y) % Height; // ensure y is within range

        return (BytesPerPixel * (Width * y + x));
    }

    public Color GetPixelColor(int x, int y)
    {
        long i = GetPixelIndex(x, y);

        if (i < 0)
        {
            return BackgroundColor;
        }

        return Color.FromArgb(_GetAlpha(i), _MainBytes[i + _R], _MainBytes[i + _G], _MainBytes[i + _B]);
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
        drawBegin();

        long i = GetPixelIndex(x, y);

        if (i < 0)
        {
            drawEnd();
            return;
        }

        _BytePointer[i + _R] = (byte)Math.Max(0, Math.Min(255, r));
        _BytePointer[i + _G] = (byte)Math.Max(0, Math.Min(255, g));
        _BytePointer[i + _B] = (byte)Math.Max(0, Math.Min(255, b));
        _BytePointer[i + _A] = 255;//(byte)Math.Max(0, Math.Min(255, a));

        drawEnd();
    }

    public void SetPixelColor(int x, int y, Color color)
    {
        SetPixelColor(x, y, color.R, color.G, color.B, color.A);
    }

    private void drawBegin()
    {
        // if (_DrawingsInProgress == 0 && !_ByteLock.IsHeldByCurrentThread)
        // {
        //     _ByteLock.Enter();
        // }

        // _DrawingsInProgress++;
    }

    private void drawEnd()
    {
        // _DrawingsInProgress--;

        // if (_DrawingsInProgress == 0 && _ByteLock.IsHeldByCurrentThread)
        // {
        //     _ByteLock.Exit();
        // }
    }
}
