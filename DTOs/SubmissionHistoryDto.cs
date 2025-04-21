namespace OnlineJudgeAPI.DTOs
{
    public class SubmissionHistoryDto
    {
        public int SubmissionId { get; set; }
        public string Username { get; set; }
        public string ProblemTitle { get; set; }
        public string Result { get; set; }
        public string code {get; set;}
        public string status {get; set;}
        public DateTime SubmittedAt { get; set; }
        public long ExecutionTimeMs { get; set; }
        public long MemoryUsageBytes { get; set; }
    }

}
