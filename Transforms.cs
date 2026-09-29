using System;
using Avalonia;
using Avalonia.Platform;

namespace Borealis;

public class Transforms
{
    public static void None(double x, double y, out double xout, out double yout)
    {
        xout = x;
        yout = y;
    }

    public static void Circular(double x, double y, out double xout, out double yout)
    {
        xout = Math.Sqrt(x * x + y * y) * Math.Cos(Math.Atan2(y, x) + .05);
        yout = Math.Sqrt(x * x + y * y) * Math.Sin(Math.Atan2(y, x) + .05);
    }

    public static void Fall(double x, double y, out double xout, out double yout)
    {
        xout = x;
        yout = y + 0.05;
    }

    public static void FlowLeft(double x, double y, out double xout, out double yout)
    {
        xout = x + 0.05;
        yout = y;
    }

    public static void FlowRight(double x, double y, out double xout, out double yout)
    {
        xout = x - 0.05;
        yout = y;
    }

    public static void Inward(double x, double y, out double xout, out double yout)
    {
        xout = x * 1.1;
        yout = y * 1.1;
    }

    public static void Outward(double x, double y, out double xout, out double yout)
    {
        xout = x * 0.90;
        yout = y * 0.90;
    }

    public static void Rise(double x, double y, out double xout, out double yout)
    {
        xout = x;
        yout = y - 0.05;
    }

    public static void Swirls(double x, double y, out double xout, out double yout)
    {
        xout = x + .15 * Math.Sin(y * Math.PI * 3.0); // + 2.0 * Math.Cos(n * k),
        yout = y + .15 * Math.Cos(x * Math.PI * 3.0); // + 2.0 * Math.Sin(n * k)
    }
}
