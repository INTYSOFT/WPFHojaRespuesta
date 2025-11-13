using System;
using System.Collections.Generic;
using System.IO;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

internal static class DebugImageExporter
{
    private static readonly Scalar HighlightColor = new(0, 255, 0);
    private const string BaseDirectory = @"d:\depurar";
    private const string AnswersDirectoryName = "respuestas";
    private const string DniDirectoryName = "DNI";

    public static void SaveAnswers(Mat originalPage, IReadOnlyCollection<Rect> regions, int pageNumber)
    {
        SaveDebugImage(originalPage, regions, pageNumber, AnswersDirectoryName, "respuestas");
    }

    public static void SaveDni(Mat originalPage, IReadOnlyCollection<Rect> regions, int pageNumber)
    {
        SaveDebugImage(originalPage, regions, pageNumber, DniDirectoryName, "dni");
    }

    private static void SaveDebugImage(Mat page, IReadOnlyCollection<Rect> regions, int pageNumber, string directoryName, string filePrefix)
    {
        if (regions.Count == 0)
        {
            return;
        }

        try
        {
            var targetDirectory = Path.Combine(BaseDirectory, directoryName);
            Directory.CreateDirectory(targetDirectory);
            using var canvas = PrepareCanvas(page);
            foreach (var rect in regions)
            {
                Cv2.Rectangle(canvas, rect, HighlightColor, 2);
            }

            var fileName = $"{filePrefix}_pagina_{pageNumber:000}.png";
            var destination = Path.Combine(targetDirectory, fileName);
            Cv2.ImWrite(destination, canvas);
        }
        catch (Exception)
        {
            // Ignoramos los errores de depuración para no afectar el flujo principal.
        }
    }

    private static Mat PrepareCanvas(Mat source)
    {
        if (source.Channels() == 1)
        {
            var canvas = new Mat();
            Cv2.CvtColor(source, canvas, ColorConversionCodes.GRAY2BGR);
            return canvas;
        }

        return source.Clone();
    }
}
