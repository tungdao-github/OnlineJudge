namespace OnlineJudgeAPI.DTOs
{
    public class ContestJoinRequest
    {
        public string Code { get; set; }
        public string Password { get; set; }
    }

    public class LeaderboardEntryDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public double TotalScore { get; set; }
        public DateTime LastSubmission { get; set; }
    }

}
