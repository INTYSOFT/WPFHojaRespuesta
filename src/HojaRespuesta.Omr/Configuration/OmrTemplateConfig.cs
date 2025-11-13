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
        // La hoja utilizada por la academia tiene el bloque de DNI en la parte
        // izquierda de la página. Ajustamos el rectángulo normalizado para que
        // abarque únicamente las columnas de burbujas (y no los cuadros donde
        // se escribe a mano) para que el cálculo de los dígitos se haga sobre
        // las 10 filas reales de opciones.
        DniRegion = new NormalizedRect(0.055, 0.23, 0.24, 0.34),
        // El bloque de preguntas ocupa la parte derecha con cuatro columnas de
        // 20 ítems cada una. Cada columna contiene exactamente 5 alternativas
        // (A-E) alineadas de forma horizontal. El alto de la región se calcula
        // para que cada fila corresponda a una pregunta y, de este modo, el
        // lector pueda separar la imagen únicamente dividiendo en partes
        // iguales sin depender de detecciones adicionales.
        AnswerColumns = new List<AnswerColumnConfig>
        {
            new()
            {
                QuestionStartNumber = 1,
                Questions = 20,
                Region = new NormalizedRect(0.335, 0.16, 0.13, 0.70)
            },
            new()
            {
                QuestionStartNumber = 21,
                Questions = 20,
                Region = new NormalizedRect(0.480, 0.16, 0.13, 0.70)
            },
            new()
            {
                QuestionStartNumber = 41,
                Questions = 20,
                Region = new NormalizedRect(0.625, 0.16, 0.13, 0.70)
            },
            new()
            {
                QuestionStartNumber = 61,
                Questions = 20,
                Region = new NormalizedRect(0.770, 0.16, 0.13, 0.70)
            }
        }
    };
}
