//using Microsoft.AspNetCore.Mvc;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using OnlineJudgeAPI.Services;
//using System;

//[Route("api/[controller]")]
//[ApiController]
//public class ContestController : ControllerBase
//{
//    private readonly AppDbContext _context;

//    public ContestController(AppDbContext context)
//    {
//        _context = context;
//    }

//    // GET: api/contest/{contestId}/problems
//    [HttpGet("{contestId}/problems")]
//    public async Task<IActionResult> GetContestProblems(int contestId)
//    {
//        var problems = await _context.ContestProblems
//            .Where(cp => cp.ContestId == contestId)
//            .Select(cp => new
//            {
//                cp.Problem.Id,
//                cp.Problem.Title
//            })
//            .ToListAsync();

//        if (problems == null || !problems.Any())
//        {
//            return NotFound("No problems found for this contest.");
//        }

//        return Ok(problems);
//    }
//    public class SubmissionRequest2
//    {
//        public int ContestId { get; set; }
//        public int ProblemId { get; set; }
//        public string Language { get; set; }
//        public string Code { get; set; }
//    }
//    // POST: api/contest/submit
//    [HttpPost("submit")]
//    public async Task<IActionResult> SubmitSolution([FromBody] SubmissionRequest2 submission)
//    {
//        if (!ModelState.IsValid)
//        {
//            return BadRequest(ModelState);
//        }

//        var newSubmission = new Submission
//        {
//            UserId = submission.usesId,
//            ContestID = submission.ContestId,
//            ProblemId = submission.ProblemId,
//            Code = submission.Code,
//            Language = submission.Language,
//            SubmittedAt = DateTime.UtcNow
//        };

//        _context.Submissions.Add(newSubmission);
//        await _context.SaveChangesAsync();

//        // Gọi dịch vụ chấm điểm ở đây (ví dụ: JudgeService)
//        var judgeService = new JudgeService();
//        var problem = await _context.Problems.FindAsync(submission.ProblemId);
//        var testCases = await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

//        foreach (var testCase in testCases)
//        {
//            var isCorrect = await judgeService.EvaluateAsync(submission.Code, testCase.Input, testCase.ExpectedOutput);
//            if (!isCorrect)
//            {
//                newSubmission.IsCorrect = false;
//                break;
//            }
//            newSubmission.IsCorrect = true;
//        }

//        await _context.SaveChangesAsync();

//        return Ok(new { Message = "Submission received and evaluated.", newSubmission.IsCorrect });
//    }
//    // POST: api/contest/finish
//    [HttpPost("finish")]
//    public async Task<IActionResult> FinishContest([FromBody] FinishContestRequest request)
//    {
//        var contest = await _context.Contests.FindAsync(request.ContestId);
//        if (contest == null)
//        {
//            return NotFound("Contest not found.");
//        }

//        if (DateTime.UtcNow < contest.EndTime)
//        {
//            return BadRequest("Contest has not ended yet.");
//        }

//        var leaderboard = await _context.Submissions
//            .Where(s => s.ContestId == request.ContestId && s.IsCorrect)
//            .GroupBy(s => s.UserId)
//            .Select(g => new
//            {
//                UserId = g.Key,
//                Score = g.Count(),
//                LastSubmissionTime = g.Max(s => s.SubmitTime)
//            })
//            .OrderByDescending(l => l.Score)
//            .ThenBy(l => l.LastSubmissionTime)
//            .ToListAsync();

//        return Ok(leaderboard);
//    }

//}
