public class ExamPaper
{
    public int Id { get; set; }
    public string? Code { get; set; } // A1, A2, B1...
    public int ExamRoomId { get; set; }
    public ExamRoom ExamRoom { get; set; }

    public ICollection<ExamPaperProblem> Problems { get; set; }
}
