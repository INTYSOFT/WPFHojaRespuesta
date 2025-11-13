using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

internal static class ImagePreprocessor
{
    public static Mat PrepareBinary(Mat source)
    {
        var gray = new Mat();
        if (source.Channels() > 1)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            gray = source.Clone();
        }

        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
        var binary = new Mat();
        Cv2.AdaptiveThreshold(blurred, binary, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, 31, 5);
        return binary;
    }
}
