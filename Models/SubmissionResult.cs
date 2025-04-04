namespace OnlineJudgeAPI.Models
{
    // Models/SubmissionResult.cs
    public class SubmissionResult
    {
        public int Id { get; set; }
        public string Status { get; set; } // Passed, Failed, etc.
        public string Details { get; set; } // Error messages or details
        public double ExecutionTime { get; set; } // Time taken to execute
        public double MemoryUsed { get; set; }

        public string result { get; set; }// Memory used
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
