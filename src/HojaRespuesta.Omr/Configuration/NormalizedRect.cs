using OpenCvSharp;

namespace HojaRespuesta.Omr.Configuration;

public readonly record struct NormalizedRect(double X, double Y, double Width, double Height)
{
    public Rect ToPixelRect(int imageWidth, int imageHeight)
    {
        var x = (int)Math.Round(X * imageWidth);
        var y = (int)Math.Round(Y * imageHeight);
        var width = (int)Math.Round(Width * imageWidth);
        var height = (int)Math.Round(Height * imageHeight);
        width = Math.Max(1, Math.Min(imageWidth - x, width));
        height = Math.Max(1, Math.Min(imageHeight - y, height));
        return new Rect(x, y, width, height);
    }
}
