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
        public int? ContestId { get; set; }
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
        public int PassedTestCases { get; set; }
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
                                           Score = s.Score
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

        public class SubmissionsProblemUser {
            public int UserId {get; set;}
            public int ProblemId {get; set;}
            public string code {get; set;}
            public string status {get; set;}
            public DateTime SubmittedAt { get; set; }
        }
        [HttpGet("historyproblemuser")]
        public async Task<List<SubmissionsProblemUser>>  getHistoryByProblem(int problemId, int userId) {
            List<SubmissionsProblemUser> submissions = await _context.Submissions.Where(s => s.ProblemId  == problemId && s.UserId == userId).Select(s => new SubmissionsProblemUser {
                UserId = s.UserId,
                ProblemId = s.ProblemId,
                code = s.Code,
                status = s.Status,
                SubmittedAt = s.SubmittedAt,
                
            }).OrderByDescending(p => p.SubmittedAt).ToListAsync();
            return submissions;
        }
        [HttpGet("history")]
        // [Authorize(Roles ="User,Admin")]
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
        private int CalculateScore(List<TestCaseResult> results)
        {
            int score = 0;

            foreach (var result in results)
            {
                if (result.Error == null && result.ActualOutput == result.ExpectedOutput)
                {
                    score += 10; // Perfect score for correct output
                }
                else
                {
                    // Calculate score reduction based on time and memory usage
                    if (result.ExecutionTimeMs < 1000) score += 5; // Time penalty
                    if (result.MemoryUsageBytes < 1024 * 100) score += 3; // Memory usage penalty
                }
            }

            return score;
        }
        // private int CalculateScore(List<TestCaseResult> testCaseResults)
        // {
        //     int score = 0;

        //     foreach (var result in testCaseResults)
        //     {
        //         if (result.Error == null && result.ActualOutput == result.ExpectedOutput)
        //         {
        //             score += 10;
        //         }
        //         else
        //         {
        //             // Penalty for errors
        //             score -= 2;
        //         }
        //     }

        //     return score;
        // }
        [HttpPost("submit")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest submissionRequest) {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if(string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("User ID khong tim thay trong token");
                var problem = _context.Problems.FirstOrDefault(p => p.Id == submissionRequest.ProblemId);
                int userId = int.Parse(userIdClaim);
                var contestId = submissionRequest.contestId;
                var submission = new Submission{
                    ProblemId = submissionRequest.ProblemId,
                    Code = submissionRequest.Code,
                    Language = submissionRequest.Language,
                    Status = "Chua giai quyet ",
                    Error = string.Empty,
                    Result = "Chua xu ly",
                    UserId = userId
                };
                _context.Submissions.Add(submission);
                var testCases = await _context.TestCases.Where(t => t.ProblemId== submissionRequest.ProblemId).ToListAsync();
                var results = await ProcessTestCasesAsync(testCases, submission, submissionRequest.ConnectionId, problem.maxScore);
                submission.ContestId = contestId;
                submission.Result = _codeExecutor.Results;
                submission.Score = _codeExecutor.Score;
            submission.Score = results.Score;

            Console.WriteLine("score = " + submission.Score);
                submission.PassedTestCases = results.PassedTestCases;
                submission.TotalTestCases = testCases.Count;
                await _context.SaveChangesAsync();
                if(contestId != null) {
                    var standing = _context.ContestStandings.FirstOrDefault(s => s.ContestId == contestId && s.UserId == userId);
                    if(standing == null) {
                        standing = new ContestStanding {
                            ContestId = contestId,
                            UserId = userId,
                            TotalScore = results.Score,
                            LastUpdated = DateTime.Now
                            
                        };
                         _context.ContestStandings.Add(standing);
                    }
                    else {
                      
                            // standing.ContestId = contestId;
                            // standing.UserId = userId;
                            standing.TotalScore += results.PassedTestCases;
                            // TotalScore += results.PassedTestCases,
                            standing.LastUpdated = DateTime.Now;

                       
                        _context.ContestStandings.Update(standing);

                }
                }
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group($"contest_{contestId}")
                    .SendAsync("UpdateStandings", contestId);

                return Ok(new { message = "Submission received!", submissionId = submission.Id });
            }
            
            //         [HttpPost("submit")]
            //         public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
            //         {
            //             var userIdClaim = User.FindFirst("userId")?.Value;
            //             if (string.IsNullOrEmpty(userIdClaim))
            //                 return Unauthorized("User ID not found in token.");

            //             int userId = int.Parse(userIdClaim);
            //             var contestId = request.contestId;

            //             var submission = new Submission
            //             {
            //                 ProblemId = request.ProblemId,
            //                 Code = request.Code,
            //                 Language = request.Language,
            //                 Status = "Pending",
            //                 Error = string.Empty,
            //                 Result = "Processing",
            //                 UserId = userId
            //             };

            //             _context.Submissions.Add(submission);

            //             var cacheKey = $"testcases:{submission.ProblemId}";
            //             var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
            //                             await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

            //             var results = await ProcessTestCasesAsync(testCases, submission, request.ConnectionId);

            //             submission.ContestId = contestId;
            //             submission.Result = _codeExecutor.Results;
            //             submission.Score = results.Score;
            //             submission.Error = results.Error;

            //             // int passed = 0;
            //             // foreach (var testCase in testCases)
            //             // {
            //             //     var result = await _codeExecutor.RunCodeAndCompare(submission, testCase);
            //             //     if (result.Status == "Accepted") passed++;
            //             // }
            //             submission.ContestId = contestId;
            //             submission.Result = _codeExecutor.Results;
            //             submission.Score = results.Score;
            //             submission.Error = results.Error;
            //             submission.PassedTestCases = results.PassedTestCases;
            //             submission.TotalTestCases = testCases.Count;
            //             // submission.PassedTestCases = passed;
            //             // submission.TotalTestCases = testCases.Count;

            //             await _context.SaveChangesAsync();
            //             if(contestId != null)
            // {
            //                 var standing = await _context.ContestStandings
            //                     .FirstOrDefaultAsync(s => s.ContestId == contestId && s.UserId == userId);

            //                 if (standing == null)
            //                 {
            //                     standing = new ContestStanding
            //                     {
            //                         ContestId = contestId,
            //                         UserId = userId,
            //                         TotalScore = results.Score,
            //                         LastUpdated = DateTime.UtcNow
            //                     };
            //                     _context.ContestStandings.Add(standing);
            //                 }
            //                 else
            //                 {
            //                     standing.TotalScore += results.PassedTestCases;
            //                     standing.LastUpdated = DateTime.UtcNow;
            //                     _context.ContestStandings.Update(standing);
            //                 }

            //                 await _context.SaveChangesAsync();

            //                 await _hubContext.Clients.Group($"contest_{contestId}")
            //                     .SendAsync("UpdateStandings", contestId);
            //             }
            //             return Ok(new { message = "Submission received!", submissionId = submission.Id });
            //         }

            string NormalizeOutput(string output)
        {
            return Regex.Replace(output, @"\s+", " ") // chuẩn hóa khoảng trắng
                        .Trim()                       // bỏ đầu/cuối
                        .Replace("\r\n", "\n")        // chuẩn hóa dòng mới
                        .Normalize(NormalizationForm.FormC); // Unicode normalization
        }

        private async Task<submit> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission, string connectionId, int maxScore) {
            bool ok = true;
            var result = await _codeExecutor.RunAndCompileCodeAsync(submission.Code, testCases, submission.Language, connectionId, maxScore);
            var resultOutput = new StringBuilder();
            if(!string.IsNullOrWhiteSpace(result.CompilationError)) {
                resultOutput.Append(result.CompilationError + "&");
                submission.Status = "Runtime Error";
                submission.Error = result.CompilationError.Trim();
                foreach (var testCase in testCases) {
                    resultOutput.Append($"Testcase {testCase.Id}: Runtime Error");
                }
                return new submit {
                    ExecutionTimeMs = result.TotalExecutionTimeMs,
                    MemoryUsageBytes = result.TotalMemoryUsageBytes,
                    Error = result.CompilationError,
                    Score = CalculateScore(result.TestCaseResults),
                    IsCorrect = "false"
                };
            }
            int passedCount = 0;
            int lineIndex = 0;



                int score = (int)Math.Round(100.0 * passedCount / testCases.Count);

                submission.IsCorrect = passedCount == testCases.Count && ok ? "true" : "false";
                submission.Status = passedCount == testCases.Count && ok? "Accepted" : "Wrong Answer";
                submission.Status = _codeExecutor.status == true ? "Accepted" : "Wrong Answer"; 
                submission.Score = passedCount;
            return new submit {
                ExecutionTimeMs = result.TotalExecutionTimeMs,
                MemoryUsageBytes = result.TotalMemoryUsageBytes,
                Error = "",
                Result = _codeExecutor.Results,
                Score = score,
                IsCorrect = submission.IsCorrect,
                PassedTestCases = passedCount
            };

        }
        // private async Task<submit> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission, string connectionId)
        // {
        //     bool ok = true;
        //     var result = await _codeExecutor.RunAndCompileCodeAsync(submission.Code, testCases, submission.Language, connectionId);
        //     Console.WriteLine("Test cases returned: " + result.TestCaseResults.Count);
        //     var outputLines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        //     var resultOutput = new StringBuilder();

        //     if (!string.IsNullOrWhiteSpace(result.CompilationError))
        //     {
        //         resultOutput.Append(result.CompilationError + "&");
        //         submission.Status = "Runtime Error";
        //         submission.Error = result.CompilationError.Trim();

        //         foreach (var testCase in testCases)
        //             resultOutput.AppendLine($"Testcase {testCase.Id}: Runtime Error");

        //         return new submit
        //         {
        //             ExecutionTimeMs = result.TotalExecutionTimeMs,
        //             MemoryUsageBytes = result.TotalMemoryUsageBytes,
        //             Error = result.CompilationError,
        //             Result = resultOutput.ToString(),
        //             Score = 0,
        //             IsCorrect = "false"
        //         };
        //     }

        //     int passedCount = 0;
        //     int lineIndex = 0;



        //     int score = (int)Math.Round(100.0 * passedCount / testCases.Count);

        //     submission.IsCorrect = passedCount == testCases.Count && ok ? "true" : "false";
        //     submission.Status = passedCount == testCases.Count && ok? "Accepted" : "Wrong Answer";
        //     submission.Status = _codeExecutor.status == true ? "Accepted" : "Wrong Answer"; 
        //     submission.Score = passedCount;

        //     return new submit
        //     {
        //         ExecutionTimeMs = result.TotalExecutionTimeMs,
        //         MemoryUsageBytes = result.TotalMemoryUsageBytes,
        //         Error = "",
        //         Result = _codeExecutor.Results,
        //         Score = score,
        //         IsCorrect = submission.IsCorrect,
        //          PassedTestCases = passedCount
        //     };
        // }

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
        public int? contestId { get; set; }
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
