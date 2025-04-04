using System.ComponentModel.DataAnnotations;

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
    public string Error { get; set; } = "";

    public string Result { get; set; } // Trường mới thêm

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
