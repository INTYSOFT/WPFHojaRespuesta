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
}
