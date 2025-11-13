namespace HojaRespuesta.Omr.Configuration;

public sealed class AnswerColumnConfig
{
    public NormalizedRect Region { get; init; }
    public int Questions { get; init; }
    public int QuestionStartNumber { get; init; }
}
