using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;

namespace OnlineJudgeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContestParticipantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContestParticipantController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAvailableContests()
        {
            var now = DateTime.UtcNow;
            var contests = await _context.Contests
                .Where(c => c.StartTime <= now && c.EndTime >= now)
                .Select(c => new {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.StartTime,
                    c.EndTime
                }).ToListAsync();
                

            return Ok(contests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContestDetail(int id)
        {
            var contest = await _context.Contests
                .Include(c => c.ContestProblems)
                .ThenInclude(cp => cp.Problem)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null) return NotFound();

            return Ok(new
            {
                contest.Id,
                contest.Name,
                contest.Description,
                contest.StartTime,
                contest.EndTime,
                Problems = contest.ContestProblems.Select(cp => new {
                    cp.ProblemId,
                    cp.Problem.Title
                })
            });
        }
    }

}
