using System.Collections.Generic;

namespace HojaRespuesta.Omr.Configuration;

/// <summary>
/// Configuración de los parámetros físicos de la hoja.
/// Ajusta <see cref="DniBandHeightRatio"/> (hDni) midiendo, en una digitalización de referencia,
/// la distancia vertical entre las marcas inferiores del DNI y la primera fila de burbujas.
/// Ajusta <see cref="QuestionBlocks"/> y, para cada bloque, <see cref="QuestionBlockSettings.HeightRatio"/> (hRespCol)
/// midiendo desde la parte superior del bloque hasta las marcas rectangulares de alternativas.
/// Los umbrales <see cref="DniIntensityThreshold"/> y <see cref="AnswerIntensityThreshold"/>
/// se calibran leyendo el promedio de intensidad (0-255) de burbujas rellenadas y vacías: ubica el valor justo a mitad
/// de ambos promedios para separar tinta real de ruido.
/// </summary>
public sealed class OmrDetectionSettings
{
    public int DniDigits { get; init; } = 8;
    public int DniRows { get; init; } = 10;
    public double DniBandHeightRatio { get; init; } = 0.24; // hDni relativo al alto total de la página
    public double DniRoiSizeRatio { get; init; } = 0.012; // Tamaño del ROI respecto al alto
    public double DniIntensityThreshold { get; init; } = 140; // Intensidad máxima (0-255) para aceptar un dígito

    public int OptionsPerQuestion { get; init; } = 5;
    public double AnswerRoiSizeRatio { get; init; } = 0.013;
    public double AnswerIntensityThreshold { get; init; } = 150;
    public double AnswerMultipleMargin { get; init; } = 12;
    public IReadOnlyList<QuestionBlockSettings> QuestionBlocks { get; init; } = new List<QuestionBlockSettings>
    {
        new() { StartQuestionNumber = 1,  QuestionCount = 25, HeightRatio = 0.63 },
        new() { StartQuestionNumber = 26, QuestionCount = 25, HeightRatio = 0.63 },
        new() { StartQuestionNumber = 51, QuestionCount = 25, HeightRatio = 0.63 },
        new() { StartQuestionNumber = 76, QuestionCount = 25, HeightRatio = 0.63 }
    };

    public double MinBottomMarkAreaRatio { get; init; } = 0.00025;
    public double MaxBottomMarkAreaRatio { get; init; } = 0.0045;
    public double MinBottomMarkAspectRatio { get; init; } = 2.2;
    public double MaxBottomMarkAspectRatio { get; init; } = 10.0;
    public double BottomMarkBandHeightRatio { get; init; } = 0.22;
    public double DniMaxXRatio { get; init; } = 0.32;
    public double AnswersMinXRatio { get; init; } = 0.35;
    public bool ExportDebugImages { get; init; } = true;

    public static OmrDetectionSettings CreateDefault() => new();

    public sealed class QuestionBlockSettings
    {
        public int StartQuestionNumber { get; init; }
        public int QuestionCount { get; init; }
        public double HeightRatio { get; init; } = 0.63;
    }
}
