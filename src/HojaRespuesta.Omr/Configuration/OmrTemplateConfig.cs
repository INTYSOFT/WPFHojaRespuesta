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
    public bool depurar_imagen { get; init; } = true;

    public static OmrTemplateConfig CreateDefault() => new()
    {
        // La hoja utilizada por la academia tiene el bloque de DNI en la parte
        // izquierda de la página. Ajustamos el rectángulo normalizado para que
        // abarque únicamente las columnas de burbujas (y no los cuadros donde
        // se escribe a mano) para que el cálculo de los dígitos se haga sobre
        // las 10 filas reales de opciones.
        // La planilla vigente utiliza una franja más angosta para el bloque de
        // DNI y las burbujas se encuentran ligeramente más arriba que en el
        // diseño anterior. El rectángulo se recalculó tomando como referencia
        // una digitalización completa (ancho total ≈ 1 800 px) y midiendo la
        // matriz de 8 x 10 burbujas para que las divisiones en columnas y
        // filas coincidan con las marcas reales.
        DniRegion = new NormalizedRect(0.030, 0.135, 0.265, 0.33),
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
                // Cada columna contiene 20 preguntas. Los valores normalizados
                // se obtuvieron midiendo los vértices del bloque de respuestas
                // (zona derecha de la hoja) y dividiendo los 100 ítems en 5
                // columnas equivalentes. De esta forma los rectángulos están
                // centrados sobre las burbujas y la separación horizontal es
                // uniforme.
                Region = new NormalizedRect(0.355, 0.165, 0.11, 0.70)
            },
            new()
            {
                QuestionStartNumber = 21,
                Questions = 20,
                Region = new NormalizedRect(0.470, 0.165, 0.11, 0.70)
            },
            new()
            {
                QuestionStartNumber = 41,
                Questions = 20,
                Region = new NormalizedRect(0.585, 0.165, 0.11, 0.70)
            },
            new()
            {
                QuestionStartNumber = 61,
                Questions = 20,
                Region = new NormalizedRect(0.700, 0.165, 0.11, 0.70)
            },
            new()
            {
                QuestionStartNumber = 81,
                Questions = 20,
                Region = new NormalizedRect(0.815, 0.165, 0.11, 0.70)
            }
        }
    };
}
