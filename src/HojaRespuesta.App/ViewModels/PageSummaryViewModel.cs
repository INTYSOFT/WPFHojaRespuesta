using HojaRespuesta.App.Models;
using HojaRespuesta.Omr.Models;

namespace HojaRespuesta.App.ViewModels;

public sealed class PageSummaryViewModel
{
    public PageSummaryViewModel(PageOmrResult result, PageSource source)
    {
        Result = result;
        Source = source;
    }

    public PageOmrResult Result { get; }
    public PageSource Source { get; }

    public int PageNumber => Result.PageNumber;
    public string Dni => Result.Dni;
    public int AnswerCount => Result.Answers.Count;
}
