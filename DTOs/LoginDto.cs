namespace OnlineJudgeAPI.DTOs
{
    public class SubmissionRequest
    {
        public string Code { get; set; }
        public int ProblemId { get; set; }
        public string Language { get; set; } // E.g., "cpp"
    }
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
