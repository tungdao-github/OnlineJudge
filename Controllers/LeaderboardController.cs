using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LeaderboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("contest/{contestId}")]
    public async Task<IActionResult> GetLeaderboard(int contestId)
    {
        // Lấy submission đúng đầu tiên cho mỗi (UserId, ProblemId)
        var validSubmissions = await _context.Submissions
            .Where(s => s.ContestId == contestId && s.IsCorrect == "true")
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
    //[HttpGet("contest/{contestId}")]
    //public async Task<IActionResult> GetLeaderboard(int contestId)
    //{
    //    var submissions = await _context.Submissions
    //        .Where(s => s.ContestId == contestId && s.IsCorrect == "true")
    //        .ToListAsync();

    //    var leaderboard = submissions
    //        .GroupBy(s => s.UserId)
    //        .Select(g => new
    //        {
    //            UserId = g.Key,
    //            TotalScore = g.Sum(s => s.Score),
    //            //TotalExecutionTime = g.Sum(s => s.),
    //            EarliestSubmissionTime = g.Min(s => s.SubmittedAt)
    //        })
    //        .OrderByDescending(x => x.TotalScore)
    //        //.ThenBy(x => x.TotalExecutionTime)
    //        .ThenBy(x => x.EarliestSubmissionTime)
    //        .ToList();

    //    return Ok(leaderboard);
    //}
    //[HttpGet("{contestId}")]
    //public IActionResult GetLeaderboard(int contestId)
    //{
    //    var result = _context.Submissions
    //        .Where(s => s.ContestId == contestId && s.IsCorrect == "true")
    //        .GroupBy(s => s.UserId)
    //        .Select(g => new {
    //            UserId = g.Key,
    //            Score = g.Count(),
    //            LastSubmit = g.Max(s => s.SubmittedAt)
    //        })
    //        .OrderByDescending(x => x.Score)
    //        .ThenBy(x => x.LastSubmit)
    //        .ToList();

    //    return Ok(result);
    //}
}
