public class ExamResultStudent
{
    public int Id { get; set; }

    public int ExamRoomId { get; set; }
    public ExamRoom ExamRoom { get; set; }

    public int ExamPaperId { get; set; }
    public ExamPaper ExamPaper { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int TotalScore { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}