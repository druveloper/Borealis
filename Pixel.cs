using System;

namespace avaTest;

public class Pixel
{
    public double x, y;
    public byte r, g, b, a;

    public Pixel(double xn, double yn, byte rn, byte gn, byte bn, byte an)
    {
        x = xn;
        y = yn;
        r = rn;
        g = gn;
        b = bn;
        a = an;
    }
}
