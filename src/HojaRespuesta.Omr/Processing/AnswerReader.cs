using System.Collections.Generic;
using HojaRespuesta.Omr.Configuration;
using HojaRespuesta.Omr.Models;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

public sealed class AnswerReader
{
    public List<AnswerResult> ReadAnswers(Mat originalPage, Mat binaryPage, OmrTemplateConfig config, int pageNumber)
    {
        var answers = new List<AnswerResult>();
        List<Rect>? debugRects = null;
        if (config.depurar_imagen)
        {
            debugRects = new List<Rect>();
        }

        foreach (var column in config.AnswerColumns)
        {
            var columnRect = column.Region.ToPixelRect(originalPage.Width, originalPage.Height);
            using var columnRegion = new Mat(binaryPage, columnRect);
            answers.AddRange(ReadColumn(columnRegion, column, config, columnRect, debugRects));
        }

        if (debugRects is { Count: > 0 })
        {
            DebugImageExporter.SaveAnswers(originalPage, debugRects, pageNumber);
        }

        return answers;
    }

    private IEnumerable<AnswerResult> ReadColumn(Mat columnRegion, AnswerColumnConfig column, OmrTemplateConfig config, Rect columnRect, List<Rect>? debugRects)
    {
        var questions = Math.Max(1, column.Questions);
        var rowHeight = Math.Max(1, columnRegion.Rows / questions);

        for (int i = 0; i < questions; i++)
        {
            var questionNumber = column.QuestionStartNumber + i;
            var y = Math.Min(columnRegion.Rows - 1, i * rowHeight);
            var height = Math.Min(rowHeight, columnRegion.Rows - y);
            var rowRect = new Rect(0, y, columnRegion.Cols, height);
            using var row = new Mat(columnRegion, rowRect);
            var absoluteQuestionRect = new Rect(columnRect.X, columnRect.Y + y, rowRect.Width, rowRect.Height);
            yield return ReadQuestion(row, questionNumber, config, absoluteQuestionRect, debugRects);
        }
    }

    private AnswerResult ReadQuestion(Mat questionRegion, int questionNumber, OmrTemplateConfig config, Rect absoluteQuestionRect, List<Rect>? debugRects)
    {
        var options = Math.Max(1, config.OptionsPerQuestion);
        var optionWidth = Math.Max(1, questionRegion.Cols / options);
        double bestScore = 0;
        double secondScore = 0;
        int bestIndex = -1;
        Rect bestRect = default;
        var hasBestRect = false;

        for (int optionIndex = 0; optionIndex < options; optionIndex++)
        {
            var x = Math.Min(questionRegion.Cols - 1, optionIndex * optionWidth);
            var width = Math.Min(optionWidth, questionRegion.Cols - x);
            var optionRect = new Rect(x, 0, width, questionRegion.Rows);
            var absoluteOptionRect = new Rect(absoluteQuestionRect.X + x, absoluteQuestionRect.Y, width, absoluteQuestionRect.Height);
            using var optionCell = new Mat(questionRegion, optionRect);
            var fill = FillLevelCalculator.Compute(optionCell);
            if (fill > bestScore)
            {
                secondScore = bestScore;
                bestScore = fill;
                bestIndex = optionIndex;
                bestRect = absoluteOptionRect;
                hasBestRect = true;
            }
            else if (fill > secondScore)
            {
                secondScore = fill;
            }
        }

        char? selectedOption = null;
        if (bestIndex >= 0 && bestScore >= config.SelectionThreshold && bestScore - secondScore >= config.AmbiguityMargin)
        {
            selectedOption = (char)('A' + bestIndex);
        }

        if (debugRects is not null && hasBestRect)
        {
            debugRects.Add(bestRect);
        }

        return new AnswerResult
        {
            QuestionNumber = questionNumber,
            SelectedOption = selectedOption,
            Confidence = bestScore
        };
    }
}
