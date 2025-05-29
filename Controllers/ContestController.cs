using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using System;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
public class CreateContestRequest
{
    public string Title { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; }
    public List<ProblemScoreDto> Problems { get; set; }
}

public class ProblemScoreDto
{
    public int ProblemId { get; set; }
    public int Score { get; set; }
}
public class ContestDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int> ProblemIds { get; set; } = new();
    }
[ApiController]
[Route("api/[controller]")]
public class ContestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContestController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public IActionResult GetContests()
    {
        var contests = _context.Contests.ToList();
        return Ok(contests);
    }
    
    // GET: api/contest/5
    [HttpGet("{id}")]
    public IActionResult GetContest(int id)
    {
        var contest = _context.Contests.FirstOrDefault(c => c.Id == id);
        if (contest == null)
            return NotFound();

        return Ok(contest);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContest(int id, [FromBody] Contest updated)
    {
        var contest = _context.Contests.FirstOrDefault(c => c.Id == id);
        if (contest == null)
            return NotFound();

        contest.Name = updated.Name;
        contest.StartTime = updated.StartTime;
        contest.EndTime = updated.EndTime;
        contest.Description = updated.Description;

        await _context.SaveChangesAsync();
        return Ok(contest);
    }

    // DELETE: api/contest/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContest(int id)
    {
        var contest = _context.Contests.FirstOrDefault(c => c.Id == id);
        if (contest == null)
            return NotFound();

        _context.Contests.Remove(contest);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    
    [HttpPost("create")]
    // [Authorize(Roles = "Admin")]
    //
    // [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CreateContest(CreateContestRequest request)
    {
        var contest = new Contest
        {
            Name = request.Title,
            StartTime = request.StartTime,
            EndTime = request.EndTime,

            ContestProblems = request.Problems.Select(pid => new ContestProblem
            {
                ProblemId = pid.ProblemId,
                Score = pid.Score // default
            }).ToList(),
            Description = request.Description
        };

        _context.Contests.Add(contest);
        await _context.SaveChangesAsync();
        return Ok(contest);
    }

    [HttpPost("{contestId}/join")]
    public async Task<IActionResult> JoinContest(int contestId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (_context.ContestParticipants.Any(p => p.ContestId == contestId && p.UserId == userId))
            return BadRequest("Already joined");

        var participant = new ContestParticipant
        {
            ContestId = contestId,
            UserId = userId,
        };

        _context.ContestParticipants.Add(participant);
        await _context.SaveChangesAsync();
        return Ok("Joined");
    }
    [HttpGet("{contestId}/problems")]
    public async Task<IActionResult> GetProblemsInContest(int contestId)
    {
        var contest = await _context.Contests
            .Include(c => c.ContestProblems)
            .ThenInclude(cp => cp.Problem)
            .FirstOrDefaultAsync(c => c.Id == contestId);

        if (contest == null)
            return NotFound("Contest not found");

        var problems = contest.ContestProblems
           
            .Select((cp, index) => new
            {
                ProblemId = cp.Problem.Id,
                Title = cp.Problem.Title,
                Description = cp.Problem.Description.Length > 100
                    ? cp.Problem.Description.Substring(0, 100) + "..."
                    : cp.Problem.Description,
                
                Label = ((char)('A' + index)).ToString(), // A, B, C...
                MaxScore = cp.Score
            })
            .ToList();

        return Ok(problems);
    }

    [HttpGet("{id}/standings")]
    public async Task<IActionResult> GetStandings(int id)
    {
        var standings = await _context.ContestStandings
            .Where(cs => cs.ContestId == id)
            .OrderByDescending(cs => cs.TotalScore)
            .ThenBy(cs => cs.LastUpdated)
            .Select(cs => new
            {
                cs.UserId,
                Username = cs.User.Username,
                cs.TotalScore,
                cs.LastUpdated
            })
            .ToListAsync();

        return Ok(standings);
    }
}
