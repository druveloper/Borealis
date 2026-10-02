using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
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
    protected static readonly JsonArray _ValidMathWords = (JsonArray) JsonArray.Parse(File.ReadAllText("GpuCode/Reference/MathFunctions.json"));

    
    public GpuDrawer(byte[] bitmapData, int width, int height)
    {
        Host_BitmapData = bitmapData;
        _Width = width;
        _Height = height;
    }

    public abstract IOverlay NewOverlay(string R_Function, string G_Function, string B_Function);

    public abstract ITransform NewTransform(string X_Function, string Y_Function);

    public abstract void DrawOverlay(IOverlay overlay, Moon? moon = null);
    
    public abstract void DrawTransform(ITransform transform);

    protected static string cleanMathExpression(string mathExpression)
    {
        mathExpression = mathExpression.Replace("_", "");
        mathExpression = Regex.Replace(mathExpression, "\\s+", " ");

        return mathExpression;
    }

    protected static bool validateMathExpression(string mathExpression, string[] specialWords)
    {
        var validCharsRegEx = new Regex("[^a-zA-Z0-9. ()*/%+-]");

        var invalidCharsFound = validCharsRegEx.Matches(mathExpression);

        if (invalidCharsFound.Count > 0)
        {
            var positions = invalidCharsFound.Select((m) => m.Index);
            var invalidChars = invalidCharsFound.Select((m) => m.Value[0])
                .Distinct().Select((c) => {
                    if (char.IsSymbol(c) || char.IsPunctuation(c))
                    {
                        return c.ToString();
                    }
                    else
                    {
                        return "0x" + char.GetNumericValue(c).ToString("X2");
                    }
                });
            
            string exampleDescription = "";
            if (positions.Count() <= 5)
            {
                exampleDescription = $" ({String.Join(",", invalidChars)})";
            }

            string positionDescription = $"from positions {positions.Min()} to {positions.Max()}";
            if (positions.Count() == 1)
            {
                positionDescription = $"at position {positions.FirstOrDefault()}";
            }
            else if (positions.Count() == 2)
            {
                positionDescription = $"at positions {positions.Min()} and {positions.Max()}";
            }

            throw new ApplicationException($"Found invalid characters{exampleDescription} in math expression {positionDescription}.");
        }



        return true;
    }
}
