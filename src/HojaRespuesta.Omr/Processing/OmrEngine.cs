using System.Collections.Generic;
using HojaRespuesta.Omr.Configuration;
using HojaRespuesta.Omr.Models;
using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

public sealed class OmrEngine
{
    private readonly OmrSheetProcessor _processor = new();
    private readonly OmrDetectionSettings _defaultSettings;

    public OmrEngine()
        : this(OmrDetectionSettings.CreateDefault())
    {
    }

    public OmrEngine(OmrDetectionSettings settings)
    {
        _defaultSettings = settings;
    }

    public PageOmrResult ProcessPage(Mat pageImage, int pageNumber, OmrDetectionSettings? settings = null)
    {
        var activeSettings = settings ?? _defaultSettings;
        return _processor.ProcessPage(pageImage, pageNumber, activeSettings);
    }

    /// <summary>
    /// Procesa varias páginas consecutivas (por ejemplo, las imágenes exportadas de un PDF).
    /// Cada tupla debe proveer el <see cref="Mat"/> ya cargado y el número de página en el documento.
    /// </summary>
    public IEnumerable<PageOmrResult> ProcessPages(IEnumerable<(Mat Image, int PageNumber)> pages, OmrDetectionSettings? settings = null)
    {
        var activeSettings = settings ?? _defaultSettings;
        foreach (var (image, pageNumber) in pages)
        {
            yield return _processor.ProcessPage(image, pageNumber, activeSettings);
        }
    }
}
