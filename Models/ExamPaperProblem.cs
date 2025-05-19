using OnlineJudgeAPI.Models;

public class ExamPaperProblem
{
    public int Id { get; set; }
    public int ExamPaperId { get; set; }
    public ExamPaper ExamPaper { get; set; }

    public int ProblemId { get; set; }
    public Problem Problem { get; set; }

    public string? DisplayOrder { get; set; } // A, B, C...
}
