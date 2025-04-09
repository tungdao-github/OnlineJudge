using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using System;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Models;
using Microsoft.AspNetCore.SignalR;

[Route("api/[controller]")]
[ApiController]
public class ContestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContestController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpPost("create")]
    public async Task<IActionResult> CreateContest(CreateContestRequest request)
    {
        var contest = new Contest
        {
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Description = request.Description,
            ContestProblems = request.ProblemIds.Select(pid => new ContestProblem { ProblemId = pid.problemId }).ToList()
        };

        _context.Contests.Add(contest);
        await _context.SaveChangesAsync();
        return Ok(contest);
    }
    // GET: api/contest/{contestId}/problems

    [HttpGet("api/contests")]
    public async Task<IActionResult> GetAllContests()
    {
        var contests = await _context.Contests.ToListAsync();
        return Ok(contests);
    }

    [HttpGet("api/contests/{id}")]
    public async Task<IActionResult> GetContestDetail(int id)
    {
        var contest = await _context.Contests
            .Include(c => c.ContestProblems)
            .ThenInclude(cp => cp.Problem)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contest == null)
            return NotFound();

        return Ok(contest);
    }
    [HttpGet("api/contests/{contestId}/submissions")]
    public async Task<IActionResult> GetSubmissionsInContest(int contestId, int? userId = null)
    {
        var submissions = _context.Submissions
            .Where(s => s.ContestId == contestId);

        if (userId != null)
            submissions = submissions.Where(s => s.UserId == userId);

        var result = await submissions
            .Include(s => s.Problem)
            .Include(s => s.User)
            .ToListAsync();

        return Ok(result);
    }
    //[HttpGet("api/contests/{contestId}/leaderboard")]
    //public async Task<IActionResult> GetLeaderboard(int contestId)
    //{
    //    var leaderboard = await _context.Submissions
    //        .Where(s => s.ContestId == contestId && s.Passed == true)
    //        .GroupBy(s => new { s.UserId, s.User.UserName })
    //        .Select(g => new {
    //            g.Key.UserId,
    //            g.Key.UserName,
    //            TotalScore = g.Sum(s => s.Score)
    //        })
    //        .OrderByDescending(x => x.TotalScore)
    //        .ToListAsync();

    //    // Gửi SignalR nếu cần cập nhật real-time
    //    await _hubContext.Clients.All.SendAsync("UpdateLeaderboard", leaderboard);

    //    return Ok(leaderboard);
    //}
}
