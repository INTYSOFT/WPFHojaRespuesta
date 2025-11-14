using OpenCvSharp;

namespace HojaRespuesta.Omr.Processing;

internal readonly record struct BottomMark(Rect BoundingRect, Point2f Center, double Area);
