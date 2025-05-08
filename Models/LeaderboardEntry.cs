using OnlineJudgeAPI.Models;
public class LeaderboardEntry
{
    public int Id { get; set; }
    public int ContestId { get; set; }
    public int UserId { get; set; }
    public int TotalScore { get; set; }
    public DateTime LastUpdated { get; set; }

    public Contest Contest { get; set; }
    public User User { get; set; }
}
