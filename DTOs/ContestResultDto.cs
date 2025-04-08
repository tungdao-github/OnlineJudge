namespace OnlineJudgeAPI.DTOs
{
    public class ContestResultDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int TotalCorrect { get; set; }
        public DateTime? FirstCorrectSubmissionTime { get; set; }
    }

}
