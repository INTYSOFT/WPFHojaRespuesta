using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

internal static class FillLevelCalculator
{
    public static double Compute(Mat region)
    {
        if (region.Width == 0 || region.Height == 0)
        {
            return 0;
        }

        using var gray = new Mat();
        if (region.Channels() > 1)
        {
            Cv2.CvtColor(region, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            using var clonedRegion = region.Clone();
            clonedRegion.CopyTo(gray); // Fix: Copy the clonedRegion to gray instead of assigning directly
        }

        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(3, 3), 0);
        using var binary = new Mat();
        Cv2.Threshold(blurred, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        var filled = Cv2.CountNonZero(binary);
        return filled / (double)(region.Width * region.Height);
    }
}
