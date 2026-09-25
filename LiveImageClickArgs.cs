using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Borealis;

public class LiveImageClickArgs : RoutedEventArgs
{
    public Point MousePoint {get; set;}
    public MouseButton MouseButton {get; set;}

    public LiveImageClickArgs(RoutedEvent routedEvent, int x, int y, MouseButton button) : base(routedEvent)
    {
        MousePoint = new Point(x, y);
        MouseButton = button;
    }
}
