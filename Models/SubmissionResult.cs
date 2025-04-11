namespace OnlineJudgeAPI.Models
{
    // Models/SubmissionResult.cs
    public class SubmissionResult2
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string? Details { get; set; }
        public double ExecutionTime { get; set; }
        public double MemoryUsed { get; set; }
        public string output { get; set; }
        public string ExpectedOutput { get; set; }
        public string error { get; set; }
        public string[] result { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsCorrect { get; set; }
        public string ActualOutput { get; set; }
    }
    public class SubmissionResult
    {
        public int Id { get; set; }
        public string Status { get; set; } 
        public string? Details { get; set; } 
        public double ExecutionTime { get; set; }
        public double MemoryUsed { get; set; }
        
        public string[]  result { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
    public enum SubmissionStatus
    {
        Pending,
        Accepted,
        WrongAnswer,
        TimeLimitExceeded,
        RuntimeError
    }
}
