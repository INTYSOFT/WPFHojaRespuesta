using System.Collections.Generic;

namespace HojaRespuesta.Omr.Models;

public class PageOmrResult
{
    public int PageNumber { get; set; }
    public string Dni { get; set; } = string.Empty;
    public List<AnswerResult> Answers { get; set; } = new();
}
