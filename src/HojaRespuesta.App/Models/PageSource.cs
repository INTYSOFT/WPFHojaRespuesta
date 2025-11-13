using OpenCvSharp;

namespace HojaRespuesta.App.Models;

public sealed class PageSource : IDisposable
{
    public PageSource(int pageNumber, Mat image)
    {
        PageNumber = pageNumber;
        Image = image;
    }

    public int PageNumber { get; }
    public Mat Image { get; }

    public void Dispose()
    {
        Image.Dispose();
    }
}
