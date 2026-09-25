using System;
using System.ComponentModel;
using System.Numerics;
using System.Threading.Channels;
using Avalonia;
using Avalonia.Controls;

namespace avaTest;

public class CoordinateConverter
{
    public int ImageWidth;
    public int ImageHeight;
    public Point GraphSize;
    public Point GraphCenter;

    public struct ImagePixel
    {
        public int X, Y;
    }


    public CoordinateConverter(int imageWidth, int imageHeight, Point graphSize, Point graphCenter)
    {
        ImageHeight = imageHeight;
        ImageWidth = imageWidth;
        GraphSize = graphSize;
        GraphCenter = graphCenter;
    }

    public ImagePixel PointToPixel(Point graphPoint)
    {
        double graphTop = GraphCenter.Y + GraphSize.Y / 2;
        double graphLeft = GraphCenter.X - GraphSize.X / 2;

        double downFromTop = (graphTop - graphPoint.Y) / GraphSize.Y;
        double overFromLeft = (graphPoint.X - graphLeft) / GraphSize.X;

        ImagePixel pixel = new ImagePixel {
            X = (int)Math.Round(ImageWidth * overFromLeft),
            Y = (int)Math.Round(ImageHeight * downFromTop)
        };

        return pixel;
    }

    public ImagePixel PointToPixel(double x, double y)
    {
        return PointToPixel(new Point(x, y));
    }

    public Point PixelToPoint(ImagePixel pixel)
    {
        double graphTop = GraphCenter.Y + GraphSize.Y / 2;
        double graphLeft = GraphCenter.X - GraphSize.X / 2;

        double downFromTop = pixel.Y / (double) ImageHeight;
        double overFromLeft = pixel.X / (double) ImageWidth;

        return new Point(
            graphLeft + GraphSize.X * overFromLeft,
            graphTop - GraphSize.Y * downFromTop
        );
    }

    public Point PixelToPoint(int x, int y)
    {
        return PixelToPoint(new ImagePixel() {X = x, Y = y});
    }
}
