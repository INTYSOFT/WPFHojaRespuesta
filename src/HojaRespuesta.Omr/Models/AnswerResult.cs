namespace HojaRespuesta.Omr.Models;

public class AnswerResult
{
    public int QuestionNumber { get; set; }
    public char? SelectedOption { get; set; }
    public double Confidence { get; set; }
}
