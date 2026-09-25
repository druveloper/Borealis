using System;
using System.Drawing;
using Avalonia;
using Avalonia.Input;

namespace avaTest;

public class GraphPoint
{
    public double x, y;
    public byte r, g, b, a;

    public GraphPoint(double xn, double yn, byte rn, byte gn, byte bn, byte an = 255)
    {
        x = xn;
        y = yn;
        r = rn;
        g = gn;
        b = bn;
        a = an;
    }

    public GraphPoint(double xn, double yn, Color color) : this(xn, yn, color.R, color.G, color.B, color.A)
    {
        
    }

    public GraphPoint(Avalonia.Point location, Color color) : this(location.X, location.Y, color.R, color.G, color.B, color.A)
    {
        
    }
    public GraphPoint(System.Drawing.Point location, Color color) : this(location.X, location.Y, color.R, color.G, color.B, color.A)
    {
        
    }
}
