using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Constraints;
using OnlineJudgeAPI.Models;
public class Submission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProblemId { get; set; }

    [Required]
    public string Code { get; set; }

    [Required]
    public string Language { get; set; }

    public string Status { get; set; } = "Pending";
    public string Output { get; set; } = "";
    public string? Error { get; set; } = "";

    public string Result { get; set; } // Trường mới thêm
    //public long MemoryUsageBytes { get; set; } = 0;
    //public long ExecutionTimeMs { get; set; } = 0;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; }  // Assuming you have a User model
    public int? ContestId { get; set; }

    public string? IsCorrect { get; set; }
    public int? Score { get; set; }
    public int? PassedTestCases { get; set; }
    public int? TotalTestCases { get; set; }
    public Contest Contest { get; set; }
    public Problem Problem { get; set; }
}
