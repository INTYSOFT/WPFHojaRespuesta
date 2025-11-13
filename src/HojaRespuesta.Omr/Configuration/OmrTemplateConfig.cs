using System.Collections.Generic;

namespace HojaRespuesta.Omr.Configuration;

public sealed class OmrTemplateConfig
{
    public NormalizedRect DniRegion { get; init; }
    public int DniDigits { get; init; } = 8;
    public IReadOnlyList<AnswerColumnConfig> AnswerColumns { get; init; } = Array.Empty<AnswerColumnConfig>();
    public int OptionsPerQuestion { get; init; } = 5;
    public double SelectionThreshold { get; init; } = 0.25;
    public double AmbiguityMargin { get; init; } = 0.08;
    public double DniThreshold { get; init; } = 0.15;

    public static OmrTemplateConfig CreateDefault() => new()
    {
        DniRegion = new NormalizedRect(0.1, 0.12, 0.8, 0.12),
        AnswerColumns = new List<AnswerColumnConfig>
        {
            new()
            {
                QuestionStartNumber = 1,
                Questions = 30,
                Region = new NormalizedRect(0.08, 0.35, 0.38, 0.55)
            },
            new()
            {
                QuestionStartNumber = 31,
                Questions = 30,
                Region = new NormalizedRect(0.54, 0.35, 0.38, 0.55)
            }
        }
    };
}
