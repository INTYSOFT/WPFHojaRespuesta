using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

internal sealed class PreprocessedPage : IDisposable
{
    public PreprocessedPage(Mat color, Mat gray, Mat binary, IReadOnlyList<BottomMark> marks)
    {
        Color = color;
        Gray = gray;
        Binary = binary;
        BottomMarks = marks;
    }

    public Mat Color { get; }
    public Mat Gray { get; }
    public Mat Binary { get; }
    public IReadOnlyList<BottomMark> BottomMarks { get; }

    public void Dispose()
    {
        Color.Dispose();
        Gray.Dispose();
        Binary.Dispose();
    }
}
