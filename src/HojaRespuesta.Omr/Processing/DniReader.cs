using System.Collections.Generic;
using System.Text;
using HojaRespuesta.Omr.Configuration;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

public sealed class DniReader
{
    public string ReadDni(Mat originalPage, Mat binaryPage, OmrTemplateConfig config, int pageNumber)
    {
        var dniRect = config.DniRegion.ToPixelRect(originalPage.Width, originalPage.Height);
        using var dniRegion = new Mat(binaryPage, dniRect);
        var digits = Math.Max(1, config.DniDigits);
        var columnWidth = Math.Max(1, dniRegion.Width / digits);
        var builder = new StringBuilder();
        List<Rect>? debugRects = null;
        if (config.depurar_imagen)
        {
            debugRects = new List<Rect>();
        }

        for (int i = 0; i < digits; i++)
        {
            var x = Math.Min(dniRegion.Width - 1, i * columnWidth);
            var width = Math.Min(columnWidth, dniRegion.Width - x);
            var digitRect = new Rect(x, 0, width, dniRegion.Height);
            using var digitRegion = new Mat(dniRegion, digitRect);
            var absoluteDigitRect = new Rect(dniRect.X + x, dniRect.Y, width, digitRect.Height);
            builder.Append(ReadDigit(digitRegion, config.DniThreshold, absoluteDigitRect, debugRects));
        }

        if (debugRects is { Count: > 0 })
        {
            DebugImageExporter.SaveDni(originalPage, debugRects, pageNumber);
        }

        return builder.ToString();
    }

    private static char ReadDigit(Mat columnRegion, double threshold, Rect digitAbsoluteRect, List<Rect>? debugRects)
    {
        var cellHeight = Math.Max(1, columnRegion.Rows / 10);
        double bestScore = 0;
        int bestDigit = -1;
        double secondScore = 0;
        Rect bestRect = default;
        var hasBestRect = false;

        for (int digit = 0; digit < 10; digit++)
        {
            var y = Math.Min(columnRegion.Rows - 1, digit * cellHeight);
            var height = Math.Min(cellHeight, columnRegion.Rows - y);
            var cellRect = new Rect(0, y, columnRegion.Cols, height);
            var absoluteCellRect = new Rect(digitAbsoluteRect.X, digitAbsoluteRect.Y + y, digitAbsoluteRect.Width, height);
            using var cell = new Mat(columnRegion, cellRect);
            var fill = FillLevelCalculator.Compute(cell);
            if (fill > bestScore)
            {
                secondScore = bestScore;
                bestScore = fill;
                bestDigit = digit;
                bestRect = absoluteCellRect;
                hasBestRect = true;
            }
            else if (fill > secondScore)
            {
                secondScore = fill;
            }
        }

        if (bestDigit < 0 || bestScore < threshold || bestScore - secondScore < 0.02)
        {
            if (debugRects is not null && hasBestRect)
            {
                debugRects.Add(bestRect);
            }
            return '?';
        }

        if (debugRects is not null && hasBestRect)
        {
            debugRects.Add(bestRect);
        }

        return bestDigit.ToString()[0];
    }
}
