//using Microsoft.AspNetCore.Mvc;
//using OnlineJudgeAPI.Services;
//using Microsoft.EntityFrameworkCore;
//namespace OnlineJudgeAPI.Controllers
//{
//    [ApiController]
//    [Route("api/contest/{contestId}/rank")]
//    public class LeaderboardController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;

//        public LeaderboardController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetRank(int contestId)
//        {
//            var leaderboard = await _context.Submissions
//                .Where(s => s.ContestID == contestId)
//                .GroupBy(s => s.UserId)
//                .Select(g => new
//                {
//                    UserId = g.Key,
//                    Username = g.First().User.Username,
//                    TotalScore = g.Sum(s => s.Score),
//                    LastSubmission = g.Max(s => s.SubmittedAt)
//                })
//                .OrderByDescending(x => x.TotalScore)
//                .ThenBy(x => x.LastSubmission)
//                .ToListAsync();

//            return Ok(leaderboard);
//        }
//    }

//}
