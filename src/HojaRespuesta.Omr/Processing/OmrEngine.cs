using HojaRespuesta.Omr.Configuration;
using HojaRespuesta.Omr.Models;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

public sealed class OmrEngine
{
    private readonly DniReader _dniReader;
    private readonly AnswerReader _answerReader;

    public OmrEngine()
        : this(new DniReader(), new AnswerReader())
    {
    }

    public OmrEngine(DniReader dniReader, AnswerReader answerReader)
    {
        _dniReader = dniReader;
        _answerReader = answerReader;
    }

    public PageOmrResult ProcessPage(Mat pageImage, int pageNumber, OmrTemplateConfig config)
    {
        using var binary = ImagePreprocessor.PrepareBinary(pageImage);
        var dni = _dniReader.ReadDni(pageImage, binary, config);
        var answers = _answerReader.ReadAnswers(pageImage, binary, config);

        return new PageOmrResult
        {
            PageNumber = pageNumber,
            Dni = dni,
            Answers = answers
        };
    }
}
