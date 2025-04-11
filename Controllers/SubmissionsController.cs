using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Diagnostics;
using Humanizer;
using OnlineJudgeAPI.DTOs;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using OnlineJudgeAPI.Hubs;

namespace OnlineJudgeAPI.Controllers
{
    public class ContestStanding
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int UserId { get; set; }
        public int? TotalScore { get; set; } // Tổng số test case đúng
        public DateTime LastUpdated { get; set; }

        public User User { get; set; }
        public Contest Contest { get; set; }
    }
    public class ApiError
    {
        public string Message { get; set; }
        public string? Details { get; set; }
        public int StatusCode { get; set; }

        public ApiError(string message, int statusCode = 400, string? details = null)
        {
            Message = message;
            StatusCode = statusCode;
            Details = details;
        }
    }
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
    }
    class submit
        {
            public string Error { get; set; } = "";

            public string Result { get; set; } 
            public long ExecutionTimeMs { get; set; } 

            public long MemoryUsageBytes { get; set; }
            public string IsCorrect { get; set; }
        public int? Score { get; set; }
    }
    public class SubmissionDto
    {
        public int ProblemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int? ContestId { get; set; } // optional
        
    }
    [Route("api/[controller]")]
    [ApiController]
    public class SubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CodeExecutor _codeExecutor;
        private readonly ISubmissionService _submissionService;
        private readonly SubmissionQueue _submissionQueue;
        private readonly ICacheService _cache;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ContestHub> _contestHub;
        private readonly IHubContext<LeaderboardHub> _hubContext;
        public SubmissionsController(
            ApplicationDbContext context,
            CodeExecutor codeExecutor,
            ISubmissionService submissionService,
            SubmissionQueue submissionQueue,
            ICacheService cache,
            INotificationService notificationService,
            IHubContext<ContestHub> contestHub,
             IHubContext<LeaderboardHub> hubContext)
        {
            _context = context;
            _codeExecutor = codeExecutor;
            _submissionService = submissionService;
            _submissionQueue = submissionQueue;
            _cache = cache;
            _notificationService = notificationService;
            _contestHub = contestHub;
            _hubContext = hubContext;
        }
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        }
        [HttpPost("submit2")]
        public async Task<IActionResult> Submit([FromBody] SubmissionDto dto)
        {
            var submission = new Submission
            {
                UserId = GetCurrentUserId(),
                ProblemId = dto.ProblemId,
                Code = dto.Code,
                Language = dto.Language,
                ContestId = dto.ContestId,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // TODO: Gửi submission vào queue chấm
            return Ok(new { submission.Id });
        }

        //[HttpGet("GetResult/{id}")]
        //public async Task<ActionResult<SubmissionResult>> GetResult(int id)
        //{
        //    var submission = await _context.Submissions
        //                       .AsNoTracking()
        //                       .FirstOrDefaultAsync(s => s.Id == id);
        //    if (submission == null)
        //    {
        //        return NotFound("Submission not found.");
        //    }

        //    var result = await _submissionService.GetResultById(id);
        //    return Ok(result ?? new SubmissionResult { Status = "Not Available" });
        //}
        
        [HttpGet("GetResult/{id}")]
        public async Task<ActionResult<SubmissionResult>> GetResult(int id)
        {
            var result = await _context.Submissions
                                       .AsNoTracking()
                                       .Where(s => s.Id == id)
                                       .Select(s => new SubmissionResult
                                       {
                                           //string []arr = s.Result.Split("")
                                           Status = s.Status,
                                           result = Regex.Split(s.Result, "&") ,
                                           Details = s.Error,
                                           //ExecutionTime = s.ExecutionTimeMs,
                                           //MemoryUsed = s.MemoryUsageBytes
                                       })
                                       .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "Submission not found" });

            if (result == null || result.Status == "Pending" || result.Status == "Processing")
            {
                return Ok(new { message = "Submission is still being processed", submissionId = id });
            }

            return Ok(result);
        }
        [HttpGet("history")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<IEnumerable<SubmissionHistoryDto>>> GetSubmissionHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var history = await _context.Submissions
                .Include(s => s.User)
                .Include(s => s.Problem)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new SubmissionHistoryDto
                {
                    SubmissionId = s.Id,
                    Username = s.User.Username,
                    ProblemTitle = s.Problem.Title,
                    Result = s.Result,
                    SubmittedAt = s.SubmittedAt,


                })
                .ToListAsync();


            return Ok(history);
        }
        //public async Task<SubmissionResult> JudgeAsync(Submission submission)
        //{
        //    var testCases = _db.TestCases
        //        .Where(tc => tc.ProblemId == submission.ProblemId);

        //    // Nếu là contest → lọc test case thuộc contest
        //    if (submission.ContestId.HasValue)
        //    {
        //        testCases = testCases.Where(tc => tc.ContestId == submission.ContestId);
        //    }

        //    var results = await RunTestCases(submission.Code, testCases.ToList());

        //    submission.Score = results.Count(r => r.IsPassed); // hoặc tính điểm theo từng test case

        //    if (results.All(r => r.IsPassed))
        //    {
        //        submission.IsCorrect = "true";

        //        // Gửi update realtime
        //        await _hubContext.Clients
        //            .Group($"contest-{submission.ContestId}")
        //            .SendAsync("LeaderboardUpdated", submission.ContestId);
        //    }

        //    submission.Result = Serialize(results);

        //    _db.Submissions.Add(submission);
        //    await _db.SaveChangesAsync();

        //    return new SubmissionResult
        //    {
        //        Passed = results.Count(r => r.IsPassed),
        //        Total = results.Count
        //    };
        //}
        //[Authorize(Roles = "User,Admin")]
        //[Authorize]
        //[HttpPost("submit")]
        //public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
        //{

        //    //var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        //    //if (userIdClaim == null)
        //    //    return Unauthorized("User ID not found in token.");
        //    var userIdClaim = User.FindFirst("userId")?.Value;
        //    var contestId = request.contestId;
        //    if (string.IsNullOrEmpty(userIdClaim))
        //        return Unauthorized("User ID not found in token.");

        //    int userId = int.Parse(userIdClaim);
        //    //int userId = int.Parse(userIdClaim.Value);
        //    var submission = new Submission
        //    {
        //        ProblemId = request.ProblemId,
        //        Code = request.Code,
        //        Language = request.Language,
        //        Status = "Pending",
        //        Error = string.Empty,
        //        Result = "Processing",
        //        UserId = userId,
        //        //ExecutionTimeMs = 0,
        //        //MemoryUsageBytes = 0,

        //    };

        //    _context.Submissions.Add(submission);
        //    //await _context.SaveChangesAsync();

        //    // Cache test cases for the problem
        //    var cacheKey = $"testcases:{submission.ProblemId}";
        //    var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
        //                    await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

        //    //await _cache.SetAsync(cacheKey, testCases, TimeSpan.FromMinutes(10));

        //    // Process test cases concurrently but with limited parallelism
        //    var results = await ProcessTestCasesAsync(testCases, submission, request.ConnectionId);
        //    submission.ContestId = contestId;
        //    // Update submission result
        //    submission.Result = results.Result;
        //    submission.Score = results.Score;
        //    //submission.ExecutionTimeMs = results.ExecutionTimeMs;
        //    //submission.MemoryUsageBytes = results.MemoryUsageBytes;
        //    submission.Error = results.Error;
        //    Console.WriteLine(results.Result + " " + results.ExecutionTimeMs + " " + results.MemoryUsageBytes + " " + results.Error);
        //    if (submission.IsCorrect == "true" && submission.ContestId != null)
        //    {
        //        await _hubContext.Clients
        //            .Group($"contest-{submission.ContestId}")
        //            .SendAsync("LeaderboardUpdated", submission.ContestId);
        //    }
        //    int passed = 0;
        //    foreach (var testCase in testCases)
        //    {
        //        var result = await _codeExecutor.RunCodeAndCompare(submission, testCase);
        //        if (result.Status == "Accepted") passed++;
        //    }

        //    submission.PassedTestCases = passed;
        //    submission.TotalTestCases = testCases.Count;

        //    await _context.SaveChangesAsync();

        //    // Nếu trong contest thì cập nhật standings
        //    if (contestId != null)
        //    {
        //        var standing = await _context.ContestStandings
        //            .FirstOrDefaultAsync(s => s.ContestId == contestId && s.UserId == userId);

        //        if (standing == null)
        //        {
        //            standing = new ContestStanding
        //            {
        //                ContestId = contestId,
        //                UserId = userId,
        //                TotalScore = results.Score,
        //                LastUpdated = DateTime.UtcNow
        //            };
        //            _context.ContestStandings.Add(standing);
        //        }
        //        else
        //        {
        //            standing.TotalScore += passed;
        //            standing.LastUpdated = DateTime.UtcNow;
        //            _context.ContestStandings.Update(standing);
        //        }

        //        await _context.SaveChangesAsync();

        //        // Gửi real-time standings update
        //        await _hubContext.Clients.Group($"contest_{contestId}").SendAsync("UpdateStandings", contestId);
        //    }

        //    await _context.SaveChangesAsync();
        //    //await _contestHub.Clients.Group($"contest-{request.ContestId}")
        //        //.SendAsync("ReceiveTestResult", new
        //        //{
        //        //    SubmissionId = submission.Id,
        //        //    ProblemId = submission.ProblemId,
        //        //    Output = results.Result,
        //        //    //Passed = result.Score == 100
        //        //});
        //    // Send real-time notification
        //    await _notificationService.SendSubmissionUpdate(submission);

        //    return Ok(new { message = "Submission received!", submissionId = submission.Id });
        //}

        //private async Task<submit> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission, string ConnectionId)
        //{
        //    bool ok = true;
        //    var allInputs = string.Join('\n', testCases.Select(tc => tc.Input.Trim()));
        //    //var stopwatch = Stopwatch.StartNew();
        //    //var connectionId = _context.ConnectionId;
        //    var result = await _codeExecutor.RunAndCompileCodeAsync( submission.Code,  testCases, submission.Language, ConnectionId);
        //    //stopwatch.Stop();

        //    var outputLines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        //    var resultOutput = new StringBuilder();
        //    bool hasWrongAnswer = false;
        //    Console.WriteLine("result.CompilationError = " + result.CompilationError + " " + result.StandardError);
        //    if (!string.IsNullOrWhiteSpace(result.CompilationError))
        //    {
        //        resultOutput.Append(result.CompilationError + "&");
        //        //result.CompilationError = re
        //        submission.Status = "Runtime Error";
        //        submission.Error = result.CompilationError.Trim();


        //        foreach (var testCase in testCases)
        //        {
        //            resultOutput.AppendLine($"Testcase {testCase.Id}: Runtime Error");
        //            ok = false;
        //        }
        //        return new submit { ExecutionTimeMs = result.TotalExecutionTimeMs, MemoryUsageBytes = result.TotalExecutionTimeMs, Error=result.CompilationError, Result = result.CompilationError};
        //    }
        //    int lineIndex = 0;
        //    //for (int i = 0; i < testCases.Count; i++)
        //    //{
        //    //    var expected = testCases[i].ExpectedOutput.Trim().Normalize(NormalizationForm.FormC);
        //    //    var expectedLines = expected.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        //    //    var actualLines = outputLines.Skip(lineIndex).Take(expectedLines.Length).ToArray();
        //    //    lineIndex += expectedLines.Length;
        //    //    Console.WriteLine("actualLines = " + actualLines);
        //    //    var actual = string.Join('\n', actualLines).Trim().Normalize(NormalizationForm.FormC);

        //    //    Console.WriteLine("expected != actual = " + expected != actual);
        //    //    expected =  expected.Normalize();
        //    //    Console.WriteLine("expected = " + expected);
        //    //    Console.WriteLine("actual = " + actual);
        //    //    Console.WriteLine("expected -= actual : " + expected != actual);
        //    //    if (expected != actual)
        //    //    {
        //    //        resultOutput.AppendLine($"Testcase {testCases[i].Id}: Wrong Answer&");
        //    //        //resultOutput.AppendLine($"Expected: {expected}");
        //    //        //resultOutput.AppendLine($"Got: {actual}");
        //    //        hasWrongAnswer = true;
        //    //        ok = false;
        //    //    }
        //    //    else
        //    //    {
        //    //        resultOutput.AppendLine($"Testcase {testCases[i].Id}: Accepted&");
        //    //    }
        //    //}
        //    int passedCount = 0;

        //    for (int i = 0; i < testCases.Count; i++)
        //    {
        //        var expected = testCases[i].ExpectedOutput.Trim().Normalize(NormalizationForm.FormC);
        //        var expectedLines = expected.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        //        var actualLines = outputLines.Skip(lineIndex).Take(expectedLines.Length).ToArray();
        //        lineIndex += expectedLines.Length;
        //        var actual = string.Join('\n', actualLines).Trim().Normalize(NormalizationForm.FormC);

        //        if (expected != actual)
        //        {
        //            resultOutput.AppendLine($"Testcase {testCases[i].Id}: Wrong Answer&");
        //            hasWrongAnswer = true;
        //            ok = false;
        //        }
        //        else
        //        {
        //            resultOutput.AppendLine($"Testcase {testCases[i].Id}: Accepted&");
        //            passedCount++;
        //        }
        //    }

        //    int total = testCases.Count;
        //    int score = (int)Math.Round(100.0 * passedCount / total); // điểm tính theo % test case đúng

        //    submission.IsCorrect = passedCount == total ? "true" : "false";
        //    submission.Status = passedCount == total ? "Accepted" : "Wrong Answer";
        //    submission.Score = passedCount;
        //    Console.WriteLine("resultOutput" + resultOutput.ToString());
        //    if (ok) submission.IsCorrect = "true";
        //    else submission.IsCorrect = "false";
        //        submission.Status = hasWrongAnswer ? "Wrong Answer" : "Accepted";
        //    return new submit {Score = score, IsCorrect=submission.IsCorrect, Result = resultOutput.ToString(), Error = result.CompilationError};
        //}
        [HttpPost("submit")]
        public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            int userId = int.Parse(userIdClaim);
            var contestId = request.contestId;

            var submission = new Submission
            {
                ProblemId = request.ProblemId,
                Code = request.Code,
                Language = request.Language,
                Status = "Pending",
                Error = string.Empty,
                Result = "Processing",
                UserId = userId
            };

            _context.Submissions.Add(submission);

            var cacheKey = $"testcases:{submission.ProblemId}";
            var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
                            await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

            var results = await ProcessTestCasesAsync(testCases, submission, request.ConnectionId);
            submission.ContestId = contestId;
            submission.Result = results.Result;
            submission.Score = results.Score;
            submission.Error = results.Error;

            int passed = 0;
            foreach (var testCase in testCases)
            {
                var result = await _codeExecutor.RunCodeAndCompare(submission, testCase);
                if (result.Status == "Accepted") passed++;
            }

            submission.PassedTestCases = passed;
            submission.TotalTestCases = testCases.Count;

            await _context.SaveChangesAsync();

            // Update standings if contest
            if (contestId != null)
            {
                var standing = await _context.ContestStandings
                    .FirstOrDefaultAsync(s => s.ContestId == contestId && s.UserId == userId);

                if (standing == null)
                {
                    standing = new ContestStanding
                    {
                        ContestId = contestId,
                        UserId = userId,
                        TotalScore = results.Score,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.ContestStandings.Add(standing);
                }
                else
                {
                    standing.TotalScore += passed;
                    standing.LastUpdated = DateTime.UtcNow;
                    _context.ContestStandings.Update(standing);
                }

                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group($"contest_{contestId}").SendAsync("UpdateStandings", contestId);
            }

            await _notificationService.SendSubmissionUpdate(submission);

            return Ok(new { message = "Submission received!", submissionId = submission.Id });
        }

        private async Task<submit> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission, string connectionId)
        {
            bool ok = true;
            var result = await _codeExecutor.RunAndCompileCodeAsync(submission.Code, testCases, submission.Language, connectionId);
            var outputLines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var resultOutput = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(result.CompilationError))
            {
                resultOutput.Append(result.CompilationError + "&");
                submission.Status = "Runtime Error";
                submission.Error = result.CompilationError.Trim();

                foreach (var testCase in testCases)
                    resultOutput.AppendLine($"Testcase {testCase.Id}: Runtime Error");

                return new submit
                {
                    ExecutionTimeMs = result.TotalExecutionTimeMs,
                    MemoryUsageBytes = result.TotalMemoryUsageBytes,
                    Error = result.CompilationError,
                    Result = resultOutput.ToString(),
                    Score = 0,
                    IsCorrect = "false"
                };
            }

            int passedCount = 0;
            int lineIndex = 0;

            for (int i = 0; i < testCases.Count; i++)
            {
                var expected = testCases[i].ExpectedOutput.Trim().Normalize(NormalizationForm.FormC);
                var expectedLines = expected.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var actualLines = outputLines.Skip(lineIndex).Take(expectedLines.Length).ToArray();
                lineIndex += expectedLines.Length;

                var actual = string.Join('\n', actualLines).Trim().Normalize(NormalizationForm.FormC);

                if (expected != actual)
                {
                    resultOutput.AppendLine($"Testcase {testCases[i].Id}: Wrong Answer&");
                    ok = false;
                }
                else
                {
                    resultOutput.AppendLine($"Testcase {testCases[i].Id}: Accepted&");
                    passedCount++;
                }
            }

            int score = (int)Math.Round(100.0 * passedCount / testCases.Count);

            submission.IsCorrect = passedCount == testCases.Count && ok ? "true" : "false";
            submission.Status = passedCount == testCases.Count && ok? "Accepted" : "Wrong Answer";
            submission.Score = passedCount;

            return new submit
            {
                ExecutionTimeMs = result.TotalExecutionTimeMs,
                MemoryUsageBytes = result.TotalMemoryUsageBytes,
                Error = "",
                Result = resultOutput.ToString(),
                Score = score,
                IsCorrect = submission.IsCorrect
            };
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetSubmissions()
        {
            var submissions = await _context.Submissions.AsNoTracking().ToListAsync();
            return Ok(submissions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Submission>> GetSubmission(int id)
        {
            var submission = await _context.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            return submission ?? (ActionResult<Submission>)NotFound();
        }

        [HttpPost("judge/{id}")]
        public async Task<IActionResult> JudgeSubmission(int id, [FromBody] JudgeRequest request)
        {
            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null)
                return NotFound(new { message = "Submission not found" });

            submission.Status = request.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Submission judged", status = submission.Status });
        }
        [HttpGet("contest/{contestId}")]
public async Task<IActionResult> GetSubmissionsInContest(int contestId)
{
    var submissions = await _context.Submissions
        .Where(s => s.ContestId == contestId)
        .Select(s => new {
            s.Id,
            s.UserId,
            s.ProblemId,
            s.Result,
            
            s.SubmittedAt
        })
        .ToListAsync();

    return Ok(submissions);
}

    }

    // Models
    public class SubmissionRequest
    {
        public int ProblemId { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public string ConnectionId { get; set; }
        public int contestId { get; set; }
    }

    public class JudgeRequest
    {
        public string Status { get; set; }
    }

    public class CodeSubmission
    {
        public string Code { get; set; }
    }
}
