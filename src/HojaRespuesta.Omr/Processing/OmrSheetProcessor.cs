using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HojaRespuesta.Omr.Configuration;
using HojaRespuesta.Omr.Models;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

/// <summary>
/// Procesa una página individual siguiendo el flujo descrito en la especificación:
/// 1) preprocesa (gris, blur, binariza, detecta y alinea marcas inferiores),
/// 2) calcula la grilla del DNI a partir de las 8 marcas ancla y
/// 3) reconstruye cada bloque de preguntas usando las marcas rectangulares
///    alineadas con A–E. Toda la lectura se basa en los centros detectados, sin
///    coordenadas absolutas.
/// Ejemplo para una sola página en WPF:
/// <code>
/// var settings = OmrDetectionSettings.CreateDefault();
/// var processor = new OmrSheetProcessor();
/// var result = processor.ProcessPage(pageMat, pageNumber, settings);
/// </code>
/// Ejemplo para un PDF completo (cada página ya convertida a imagen):
/// <code>
/// var processor = new OmrSheetProcessor();
/// foreach (var page in pdfPages)
/// {
///     using var mat = page.Image; // generado por Pdfium o similar
///     var result = processor.ProcessPage(mat, page.PageNumber, settings);
///     // Guardar o mostrar result
/// }
/// </code>
/// </summary>
public sealed class OmrSheetProcessor
{
    public PageOmrResult ProcessPage(Mat pageImage, int pageNumber, OmrDetectionSettings settings)
    {
        using var preprocessed = Preprocess(pageImage, settings);
        var dniDebugRects = settings.ExportDebugImages ? new List<Rect>() : null;
        var dni = ReadDni(preprocessed, settings, dniDebugRects);
        if (dniDebugRects is { Count: > 0 })
        {
            DebugImageExporter.SaveDni(preprocessed.Color, dniDebugRects, pageNumber);
        }

        var answerDebugRects = settings.ExportDebugImages ? new List<Rect>() : null;
        var answers = ReadAnswers(preprocessed, settings, answerDebugRects);
        if (answerDebugRects is { Count: > 0 })
        {
            DebugImageExporter.SaveAnswers(preprocessed.Color, answerDebugRects, pageNumber);
        }

        return new PageOmrResult
        {
            PageNumber = pageNumber,
            Dni = dni,
            Answers = answers
        };
    }

    private static PreprocessedPage Preprocess(Mat pageImage, OmrDetectionSettings settings)
    {
        var color = EnsureColor(pageImage);
        var gray = new Mat();
        Cv2.CvtColor(color, gray, ColorConversionCodes.BGR2GRAY);
        var binary = ApplyBinary(gray);

        var marks = DetectBottomMarks(binary, color.Size(), settings);
        var angle = ComputeSkewAngle(marks);
        if (Math.Abs(angle) > 0.2)
        {
            var rotatedColor = Rotate(color, -angle);
            color.Dispose();
            color = rotatedColor;

            gray.Dispose();
            gray = new Mat();
            Cv2.CvtColor(color, gray, ColorConversionCodes.BGR2GRAY);

            binary.Dispose();
            binary = ApplyBinary(gray);
            marks = DetectBottomMarks(binary, color.Size(), settings);
        }

        return new PreprocessedPage(color, gray, binary, marks);
    }

    private static Mat EnsureColor(Mat source)
    {
        if (source.Channels() == 3)
        {
            return source.Clone();
        }

        var destination = new Mat();
        if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, destination, ColorConversionCodes.BGRA2BGR);
        }
        else
        {
            Cv2.CvtColor(source, destination, ColorConversionCodes.GRAY2BGR);
        }

        return destination;
    }

    private static Mat ApplyBinary(Mat gray)
    {
        var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
        var binary = new Mat();
        Cv2.Threshold(blurred, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        blurred.Dispose();
        return binary;
    }

    private static List<BottomMark> DetectBottomMarks(Mat binary, Size size, OmrDetectionSettings settings)
    {
        var marks = new List<BottomMark>();
        Cv2.FindContours(binary, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        var pageArea = size.Width * size.Height;
        var minArea = settings.MinBottomMarkAreaRatio * pageArea;
        var maxArea = settings.MaxBottomMarkAreaRatio * pageArea;
        var minY = size.Height * (1.0 - settings.BottomMarkBandHeightRatio);

        foreach (var contour in contours)
        {
            var rect = Cv2.BoundingRect(contour);
            var area = rect.Width * rect.Height;
            if (area < minArea || area > maxArea)
            {
                continue;
            }

            if (rect.Y < minY)
            {
                continue;
            }

            var aspectRatio = rect.Width / (double)rect.Height;
            if (aspectRatio < settings.MinBottomMarkAspectRatio || aspectRatio > settings.MaxBottomMarkAspectRatio)
            {
                continue;
            }

            var center = new Point2f(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            marks.Add(new BottomMark(rect, center, area));
        }

        return marks.OrderBy(m => m.Center.X).ToList();
    }

    private static double ComputeSkewAngle(IReadOnlyCollection<BottomMark> marks)
    {
        if (marks.Count < 2)
        {
            return 0;
        }

        // 1) Generamos la secuencia de Point2f a partir de tus marcas
        var points = marks
            .Select(m => new Point2f(m.Center.X, m.Center.Y))
            .ToArray(); // Point2f[]

        // 2) Usamos la sobrecarga correcta de FitLine:
        //    Line2D Cv2.FitLine(IEnumerable<Point2f> points, DistanceTypes distType, double param, double reps, double aeps)
        Line2D line = Cv2.FitLine(points, DistanceTypes.L2, 0, 0.01, 0.01);

        // 3) Obtenemos el vector director de la recta
        double vx = line.Vx;
        double vy = line.Vy;

        // 4) Ángulo en grados
        double angle = Math.Atan2(vy, vx) * 180.0 / Math.PI;
        return angle;
    }




    private static Mat Rotate(Mat source, double angle)
    {
        var destination = new Mat();
        var center = new Point2f(source.Width / 2f, source.Height / 2f);
        var rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
        Cv2.WarpAffine(source, destination, rotationMatrix, source.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(255));
        return destination;
    }

    private static string ReadDni(PreprocessedPage page, OmrDetectionSettings settings, List<Rect>? debugRects)
    {
        var width = page.Gray.Width;
        var height = page.Gray.Height;
        var dniMarks = page.BottomMarks
            .Where(m => m.Center.X <= width * settings.DniMaxXRatio)
            .OrderBy(m => m.Center.X)
            .ToList();

        if (dniMarks.Count < settings.DniDigits)
        {
            return new string('?', settings.DniDigits);
        }

        var marksToUse = dniMarks.Take(settings.DniDigits).ToList();
        var yBase = marksToUse.Average(m => m.Center.Y);
        var hDni = settings.DniBandHeightRatio * height;
        var yTop = Math.Max(0, yBase - hDni);
        var stepY = hDni / settings.DniRows;
        var roiSize = Math.Max(5, (int)Math.Round(settings.DniRoiSizeRatio * height));
        var builder = new StringBuilder();

        for (int column = 0; column < settings.DniDigits; column++)
        {
            var mark = marksToUse[column];
            var xCenter = mark.Center.X;
            double bestIntensity = double.MaxValue;
            int bestDigit = -1;
            Rect bestRect = default;

            for (int row = 0; row < settings.DniRows; row++)
            {
                var yCenter = yTop + (row + 0.5) * stepY;
                var roi = CreateCenteredRect(xCenter, yCenter, roiSize, width, height);
                var intensity = SampleIntensity(page.Gray, roi);
                debugRects?.Add(roi);

                if (intensity < bestIntensity)
                {
                    bestIntensity = intensity;
                    bestDigit = row;
                    bestRect = roi;
                }
            }

            if (bestDigit < 0 || bestIntensity > settings.DniIntensityThreshold)
            {
                builder.Append('?');
            }
            else
            {
                builder.Append(bestDigit.ToString(CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private static List<AnswerResult> ReadAnswers(PreprocessedPage page, OmrDetectionSettings settings, List<Rect>? debugRects)
    {
        var results = new List<AnswerResult>();
        var width = page.Gray.Width;
        var height = page.Gray.Height;
        var options = settings.OptionsPerQuestion;
        var answerMarks = page.BottomMarks
            .Where(m => m.Center.X >= width * settings.AnswersMinXRatio)
            .OrderBy(m => m.Center.X)
            .ToList();

        var blocks = BuildAnswerBlockMarks(answerMarks, width, options, settings);
        var roiSize = Math.Max(5, (int)Math.Round(settings.AnswerRoiSizeRatio * height));

        for (int blockIndex = 0; blockIndex < settings.QuestionBlocks.Count; blockIndex++)
        {
            var block = settings.QuestionBlocks[blockIndex];
            var marks = blocks[blockIndex];
            if (marks.Count != options)
            {
                results.AddRange(CreateBlankBlock(block));
                continue;
            }

            var yBase = marks.Average(m => m.Center.Y);
            var hResp = block.HeightRatio * height;
            var yTop = Math.Max(0, yBase - hResp);
            var stepY = hResp / block.QuestionCount;

            for (int q = 0; q < block.QuestionCount; q++)
            {
                var questionNumber = block.StartQuestionNumber + q;
                var yCenter = yTop + (q + 0.5) * stepY;
                var samples = new List<(int OptionIndex, double Intensity, Rect Roi)>();

                for (int option = 0; option < options; option++)
                {
                    var xCenter = marks[option].Center.X;
                    var roi = CreateCenteredRect(xCenter, yCenter, roiSize, width, height);
                    var intensity = SampleIntensity(page.Gray, roi);
                    debugRects?.Add(roi);
                    samples.Add((option, intensity, roi));
                }

                var selected = samples
                    .Where(s => s.Intensity <= settings.AnswerIntensityThreshold)
                    .OrderBy(s => s.Intensity)
                    .ToList();

                AnswerState state;
                char? optionChar = null;
                double confidence = 0;

                if (selected.Count == 0)
                {
                    state = AnswerState.Blank;
                }
                else if (selected.Count == 1)
                {
                    state = AnswerState.Valid;
                    optionChar = (char)('A' + selected[0].OptionIndex);
                    confidence = ComputeConfidence(selected[0].Intensity, settings.AnswerIntensityThreshold);
                }
                else
                {
                    // Más de una alternativa por debajo del umbral ⇒ múltiple
                    var diff = selected[1].Intensity - selected[0].Intensity;
                    state = diff < settings.AnswerMultipleMargin ? AnswerState.Multiple : AnswerState.Valid;
                    if (state == AnswerState.Valid)
                    {
                        optionChar = (char)('A' + selected[0].OptionIndex);
                        confidence = ComputeConfidence(selected[0].Intensity, settings.AnswerIntensityThreshold);
                    }
                    else
                    {
                        optionChar = null;
                    }
                }

                results.Add(new AnswerResult
                {
                    QuestionNumber = questionNumber,
                    SelectedOption = optionChar,
                    Confidence = confidence,
                    State = state
                });
            }
        }

        return results;
    }

    private static List<List<BottomMark>> BuildAnswerBlockMarks(IReadOnlyList<BottomMark> marks, int width, int optionsPerQuestion, OmrDetectionSettings settings)
    {
        var targetBlockCount = settings.QuestionBlocks.Count;
        var blocks = new List<List<BottomMark>>();
        if (marks.Count == 0)
        {
            return Enumerable.Repeat(0, targetBlockCount).Select(_ => new List<BottomMark>()).ToList();
        }

        var splitGap = settings.AnswerBlockSplitGapRatio * width;
        var mergeGap = settings.AnswerColumnMergeGapRatio * width;
        var current = new List<BottomMark> { marks[0] };

        for (int i = 1; i < marks.Count; i++)
        {
            var previous = marks[i - 1];
            var currentMark = marks[i];
            if (currentMark.Center.X - previous.Center.X > splitGap)
            {
                blocks.Add(NormalizeBlock(current, optionsPerQuestion, mergeGap));
                current = new List<BottomMark>();
            }

            current.Add(currentMark);
        }

        blocks.Add(NormalizeBlock(current, optionsPerQuestion, mergeGap));

        while (blocks.Count > targetBlockCount)
        {
            var previousCount = blocks.Count;
            MergeClosestBlocks(blocks, optionsPerQuestion, mergeGap);
            if (blocks.Count == previousCount)
            {
                break;
            }
        }

        while (blocks.Count < targetBlockCount)
        {
            blocks.Add(new List<BottomMark>());
        }

        return blocks.Take(targetBlockCount).ToList();
    }

    private static void MergeClosestBlocks(List<List<BottomMark>> blocks, int optionsPerQuestion, double mergeGap)
    {
        if (blocks.Count <= 1)
        {
            return;
        }

        var minGap = double.MaxValue;
        var index = -1;
        for (int i = 1; i < blocks.Count; i++)
        {
            var left = blocks[i - 1];
            var right = blocks[i];
            if (left.Count == 0 || right.Count == 0)
            {
                index = i - 1;
                break;
            }

            var gap = right.First().Center.X - left.Last().Center.X;
            if (gap < minGap)
            {
                minGap = gap;
                index = i - 1;
            }
        }

        if (index < 0)
        {
            return;
        }

        var merged = blocks[index].Concat(blocks[index + 1]).OrderBy(m => m.Center.X).ToList();
        blocks[index] = NormalizeBlock(merged, optionsPerQuestion, mergeGap);
        blocks.RemoveAt(index + 1);
    }

    private static List<BottomMark> NormalizeBlock(List<BottomMark> block, int optionsPerQuestion, double mergeGap)
    {
        if (block.Count == 0)
        {
            return block;
        }

        block.Sort((a, b) => a.Center.X.CompareTo(b.Center.X));
        var merged = new List<BottomMark> { block[0] };
        for (int i = 1; i < block.Count; i++)
        {
            var previous = merged[^1];
            var current = block[i];
            if (current.Center.X - previous.Center.X <= mergeGap)
            {
                merged[^1] = current.Area > previous.Area ? current : previous;
            }
            else
            {
                merged.Add(current);
            }
        }

        if (merged.Count > optionsPerQuestion)
        {
            merged = merged
                .OrderBy(m => m.Center.X)
                .Take(optionsPerQuestion)
                .ToList();
        }

        return merged;
    }

    private static IEnumerable<AnswerResult> CreateBlankBlock(OmrDetectionSettings.QuestionBlockSettings block)
    {
        for (int i = 0; i < block.QuestionCount; i++)
        {
            yield return new AnswerResult
            {
                QuestionNumber = block.StartQuestionNumber + i,
                SelectedOption = null,
                Confidence = 0,
                State = AnswerState.Blank
            };
        }
    }

    private static Rect CreateCenteredRect(double centerX, double centerY, int size, int width, int height)
    {
        var half = size / 2.0;
        var x = (int)Math.Round(centerX - half);
        var y = (int)Math.Round(centerY - half);
        x = Math.Clamp(x, 0, Math.Max(0, width - 1));
        y = Math.Clamp(y, 0, Math.Max(0, height - 1));
        var rectWidth = Math.Max(1, Math.Min(size, width - x));
        var rectHeight = Math.Max(1, Math.Min(size, height - y));
        return new Rect(x, y, rectWidth, rectHeight);
    }

    private static double SampleIntensity(Mat gray, Rect roi)
    {
        using var region = new Mat(gray, roi);
        return Cv2.Mean(region).Val0;
    }

    private static double ComputeConfidence(double intensity, double threshold)
    {
        var value = (threshold - intensity) / threshold;
        return Math.Clamp(value, 0, 1);
    }
}
