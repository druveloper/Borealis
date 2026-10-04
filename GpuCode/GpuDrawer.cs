using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Borealis.GpuCode;

public abstract class GpuDrawer
{
    public class Moon
    {
        public Vector2 Position = Vector2.NaN;
        public Color Color = Color.Transparent;
    }

    public interface IOverlay
    {
        public void Modify(string R_Function, string G_Function, string B_Function);
    }

    public interface ITransform
    {
        public void Modify(string X_Function, string Y_Function);
    }

    public int Width { get {return _Width;} }
    public int Height { get {return _Height;} }
    public byte[] Host_BitmapData;

    protected int _Width, _Height;

    
    public GpuDrawer(byte[] bitmapData, int width, int height)
    {
        Host_BitmapData = bitmapData;
        _Width = width;
        _Height = height;
    }

    public IOverlay NewOverlay(string R_Function, string G_Function, string B_Function)
    {
        string rFunction = MathValidator.ValidateMathExpression(R_Function);
        string gFunction = MathValidator.ValidateMathExpression(G_Function);
        string bFunction = MathValidator.ValidateMathExpression(B_Function);

        return newOverlay(rFunction, gFunction, bFunction);
    }

    public ITransform NewTransform(string X_Function, string Y_Function)
    {
        string xFunction = MathValidator.ValidateMathExpression(X_Function);
        string yFunction = MathValidator.ValidateMathExpression(Y_Function);

        return newTransform(xFunction, yFunction);
    }

    public abstract void DrawOverlay(IOverlay overlay, Moon? moon = null);
    
    public abstract void DrawTransform(ITransform transform);

    protected abstract IOverlay newOverlay(string R_Function, string G_Function, string B_Function);

    protected abstract ITransform newTransform(string X_Function, string Y_Function);
}
