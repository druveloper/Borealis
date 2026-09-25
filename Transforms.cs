using System;
using Avalonia;
using Avalonia.Platform;

namespace avaTest;

public class Transforms
{
    public static Point Identity(double x, double y)
    {
        return new Point(x, y);
    }

    public static Point FlowUp(double x, double y)
    {
        return new Point(
            x,
            y - 0.01
        );
    }

    public static Point Shrink(double x, double y)
    {
        return new Point(
            x * 1.005,
            y * 1.005
        );
    }

    public static Point Custom(double x, double y)
    {
        return new Point(
            x + .15 * Math.Sin(y * Math.PI * 3.0), // + 2.0 * Math.Cos(n * k),
            y + .15 * Math.Cos(x * Math.PI * 3.0)  // + 2.0 * Math.Sin(n * k)
        );
    }
}
