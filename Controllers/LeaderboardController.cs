using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISubmissionService _submissionService;

    public LeaderboardController(ApplicationDbContext context, ISubmissionService submissionService)
    {
        _context = context;
        _submissionService = submissionService;
    }
    
    [HttpGet("contest/{contestId}")]
    public async Task<IActionResult> GetLeaderboard(int contestId)
    {
        // Lấy submission đúng đầu tiên cho mỗi (UserId, ProblemId)
        var validSubmissions = await _context.Submissions
            .Where(s => s.ContestId == contestId)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync();

        var firstAcceptedSubmissions = validSubmissions
            .GroupBy(s => new { s.UserId, s.ProblemId })
            .Select(g => g.First()) // lấy bài đúng đầu tiên của mỗi bài
            .ToList();

        // Gộp theo người dùng
        var leaderboard = firstAcceptedSubmissions
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
               
                TotalScore = g.Sum(x => (long)x.Score),
                //TotalExecutionTime = g.Sum(s => s.ExecutionTime),
                EarliestAcceptedSubmission = g.Min(s => s.SubmittedAt)
            })
            .OrderByDescending(x => x.TotalScore)
            //.ThenBy(x => x.TotalExecutionTime)
            .ThenBy(x => x.EarliestAcceptedSubmission)
            .ToList();

        return Ok(leaderboard);
    }
    

}

public class UserLeaderboard
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int? TotalScore { get; set; }
}
