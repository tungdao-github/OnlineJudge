using Microsoft.AspNetCore.SignalR;
using OnlineJudgeAPI.Services;
public interface ILeaderboardService
{
    Task UpdateLeaderboardAsync(int contestId, int userId);
}
public class LeaderboardService : ILeaderboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<LeaderboardHub> _hub;

    public LeaderboardService(ApplicationDbContext context, IHubContext<LeaderboardHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task UpdateLeaderboardAsync(int contestId, int userId)
    {
        var submissions = _context.Submissions
            .Where(s => s.ContestId == contestId && s.UserId == userId)
            .ToList();

        var grouped = submissions
            .GroupBy(s => s.ProblemId)
            .Select(g => g.Max(s => s.Score))
            .ToList();

        int totalScore = grouped.Sum( s => s.Value);

        var entry = _context.LeaderboardEntries
            .FirstOrDefault(e => e.ContestId == contestId && e.UserId == userId);

        if (entry == null)
        {
            entry = new LeaderboardEntry
            {
                ContestId = contestId,
                UserId = userId,
                TotalScore = totalScore,
                LastUpdated = DateTime.Now
            };
            _context.LeaderboardEntries.Add(entry);
        }
        else
        {
            entry.TotalScore = totalScore;
            entry.LastUpdated = DateTime.Now;
            _context.LeaderboardEntries.Update(entry);
        }

        await _context.SaveChangesAsync();

        var leaderboard = _context.LeaderboardEntries
            .Where(e => e.ContestId == contestId)
            .OrderByDescending(e => e.TotalScore)
            .Select(e => new
            {
                Username = e.User.Username,
                e.TotalScore,
                e.LastUpdated
            }).ToList();

        await _hub.Clients.Group($"contest-{contestId}").SendAsync("UpdateLeaderboard", leaderboard);
    }
}
