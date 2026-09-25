using System;
using Avalonia;
using Avalonia.Platform;

namespace Borealis;

public class Transforms
{
    public static void Identity(double x, double y, out double xout, out double yout)
    {
        xout = x;
        yout = y;
    }

    public static void FlowUp(double x, double y, out double xout, out double yout)
    {
        xout = x;
        yout = y - 0.01;
    }

    public static void Shrink(double x, double y, out double xout, out double yout)
    {
        xout = x * 1.05;
        yout = y * 1.05;
    }

    public static void Swirls(double x, double y, out double xout, out double yout)
    {
        xout = x + .15 * Math.Sin(y * Math.PI * 3.0); // + 2.0 * Math.Cos(n * k),
        yout = y + .15 * Math.Cos(x * Math.PI * 3.0); // + 2.0 * Math.Sin(n * k)
    }
}
