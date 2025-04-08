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
        public SubmissionsController(
            ApplicationDbContext context,
            CodeExecutor codeExecutor,
            ISubmissionService submissionService,
            SubmissionQueue submissionQueue,
            ICacheService cache,
            INotificationService notificationService,
            IHubContext<ContestHub> contestHub)
        {
            _context = context;
            _codeExecutor = codeExecutor;
            _submissionService = submissionService;
            _submissionQueue = submissionQueue;
            _cache = cache;
            _notificationService = notificationService;
            _contestHub = contestHub;
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
                                           ExecutionTime = s.ExecutionTimeMs,
                                           MemoryUsed = s.MemoryUsageBytes
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

        [Authorize(Roles = "User,Admin")]
        //[Authorize]
        [HttpPost("submit")]
        public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            //var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            //if (userIdClaim == null)
            //    return Unauthorized("User ID not found in token.");
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");
                                                    
            int userId = int.Parse(userIdClaim);
            //int userId = int.Parse(userIdClaim.Value);
            var submission = new Submission
            {
                ProblemId = request.ProblemId,
                Code = request.Code,
                Language = request.Language,
                Status = "Pending",
                Error = string.Empty,
                Result = "Processing",
                UserId = userId,
                ExecutionTimeMs = 0,
                MemoryUsageBytes = 0,
                
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // Cache test cases for the problem
            var cacheKey = $"testcases:{submission.ProblemId}";
            var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
                            await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

            await _cache.SetAsync(cacheKey, testCases, TimeSpan.FromMinutes(10));

            // Process test cases concurrently but with limited parallelism
            var results = await ProcessTestCasesAsync(testCases, submission, request.ConnectionId);

            // Update submission result
            submission.Result = results.Result;
            submission.ExecutionTimeMs = results.ExecutionTimeMs;
            submission.MemoryUsageBytes = results.MemoryUsageBytes;
            submission.Error = results.Error;
            Console.WriteLine(results.Result + " " + results.ExecutionTimeMs + " " + results.MemoryUsageBytes + " " + results.Error);
                
            await _context.SaveChangesAsync();
            //await _contestHub.Clients.Group($"contest-{request.ContestId}")
                //.SendAsync("ReceiveTestResult", new
                //{
                //    SubmissionId = submission.Id,
                //    ProblemId = submission.ProblemId,
                //    Output = results.Result,
                //    //Passed = result.Score == 100
                //});
            // Send real-time notification
            await _notificationService.SendSubmissionUpdate(submission);

            return Ok(new { message = "Submission received!", submissionId = submission.Id });
        }
        class submit
        {
            public string Error { get; set; } = "";

            public string Result { get; set; } 
            public long ExecutionTimeMs { get; set; } 

            public long MemoryUsageBytes { get; set; }
        }
        private async Task<submit> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission, string ConnectionId)
        {
            var allInputs = string.Join('\n', testCases.Select(tc => tc.Input.Trim()));
            var stopwatch = Stopwatch.StartNew();
            //var connectionId = _context.ConnectionId;
            var result = await _codeExecutor.RunAndCompileCodeAsync( submission.Code,  testCases, submission.Language, ConnectionId);
            stopwatch.Stop();
            
            var outputLines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var resultOutput = new StringBuilder();
            bool hasWrongAnswer = false;
            Console.WriteLine("result.CompilationError = " + result.CompilationError + " " + result.StandardError);
            if (!string.IsNullOrWhiteSpace(result.CompilationError))
            {
                resultOutput.Append(result.CompilationError + "&");
                //result.CompilationError = re
                submission.Status = "Runtime Error";
                submission.Error = result.CompilationError.Trim();
               

                foreach (var testCase in testCases)
                {
                    resultOutput.AppendLine($"Testcase {testCase.Id}: Runtime Error");
                }
                return new submit { ExecutionTimeMs = result.TotalExecutionTimeMs, MemoryUsageBytes = result.TotalExecutionTimeMs, Error=result.CompilationError, Result = result.CompilationError};
            }
            int lineIndex = 0;
            for (int i = 0; i < testCases.Count; i++)
            {
                var expected = testCases[i].ExpectedOutput.Trim().Normalize(NormalizationForm.FormC);
                var expectedLines = expected.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var actualLines = outputLines.Skip(lineIndex).Take(expectedLines.Length).ToArray();
                lineIndex += expectedLines.Length;
                Console.WriteLine("actualLines = " + actualLines);
                var actual = string.Join('\n', actualLines).Trim().Normalize(NormalizationForm.FormC);

                Console.WriteLine("expected != actual = " + expected != actual);
                expected =  expected.Normalize();
                Console.WriteLine("expected = " + expected);
                Console.WriteLine("actual = " + actual);
                Console.WriteLine("expected -= actual : " + expected != actual);
                if (expected != actual)
                {
                    resultOutput.AppendLine($"Testcase {testCases[i].Id}: Wrong Answer&");
                    //resultOutput.AppendLine($"Expected: {expected}");
                    //resultOutput.AppendLine($"Got: {actual}");
                    hasWrongAnswer = true;
                }
                else
                {
                    resultOutput.AppendLine($"Testcase {testCases[i].Id}: Accepted&");
                }
            }
            Console.WriteLine("resultOutput" + resultOutput.ToString());

            submission.Status = hasWrongAnswer ? "Wrong Answer" : "Accepted";
            return new submit { Result = resultOutput.ToString(), ExecutionTimeMs=result.TotalExecutionTimeMs, Error = result.CompilationError};
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
    }

    // Models
    public class SubmissionRequest
    {
        public int ProblemId { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public string ConnectionId { get; set; }
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
