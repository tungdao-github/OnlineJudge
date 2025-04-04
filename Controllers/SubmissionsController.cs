using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudge.Services;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineJudgeAPI.Controllers
{
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

        public SubmissionsController(
            ApplicationDbContext context,
            CodeExecutor codeExecutor,
            ISubmissionService submissionService,
            SubmissionQueue submissionQueue,
            ICacheService cache,
            INotificationService notificationService)
        {
            _context = context;
            _codeExecutor = codeExecutor;
            _submissionService = submissionService;
            _submissionQueue = submissionQueue;
            _cache = cache;
            _notificationService = notificationService;
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
                                           Status = s.Status,
                                           result = s.Result
                                       })
                                       .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "Submission not found" });

            if (string.IsNullOrEmpty(result.result) || result.Status == "Pending" || result.Status == "Processing")
            {
                return Ok(new { message = "Submission is still being processed", submissionId = id });
            }

            return Ok(result);
        }
        //[Authorize(Roles = "User,Admin")]
        [HttpPost("submit")]
        public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var submission = new Submission
            {
                ProblemId = request.ProblemId,
                Code = request.Code,
                Language = request.Language,
                Status = "Pending",
                Error = string.Empty,
                Result = "Processing"
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // Cache test cases for the problem
            var cacheKey = $"testcases:{submission.ProblemId}";
            var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
                            await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

            await _cache.SetAsync(cacheKey, testCases, TimeSpan.FromMinutes(10));

            // Process test cases concurrently but with limited parallelism
            var results = await ProcessTestCasesAsync(testCases, submission);

            // Update submission result
            submission.Result = results;
            await _context.SaveChangesAsync();

            // Send real-time notification
            await _notificationService.SendSubmissionUpdate(submission);

            return Ok(new { message = "Submission received!", submissionId = submission.Id });
        }

        private async Task<string> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission)
        {
            var results = new ConcurrentDictionary<int, (string output, string error)>();
            var maxConcurrency = 150; // Giảm từ 4 xuống 2 để kiểm tra
            var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = testCases.Select(async testCase =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine($"[DEBUG] Processing TestCase {testCase.Id}  {testCase.Input} {testCase.ExpectedOutput}...");
                    var result = await _codeExecutor.RunCodeAsync(submission.Language, submission.Code, testCase.Input);
                    Console.WriteLine("output, error = " + result.output + " "+ result.error);
                    results[testCase.Id] = result;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var resultOutput = new StringBuilder();
            bool hasRuntimeError = false;

            foreach (var (index, (output, error)) in results.OrderBy(r => r.Key))
            {
                var expectedOutput = testCases.First(tc => tc.Id == index).ExpectedOutput.Trim();
                Console.WriteLine($"[DEBUG] TestCase {index}: Expected [{expectedOutput}], Got [{output.Trim()}]");

                if (!string.IsNullOrEmpty(error))
                {
                    submission.Status = "Runtime Error";
                    submission.Error = error;
                    resultOutput.AppendLine($"{error}\nTestcase {index}: Runtime Error");
                    hasRuntimeError = true;
                    break;
                }

                if (output.Trim() != expectedOutput)
                {
                    submission.Status = "Wrong Answer";
                    resultOutput.AppendLine($"Testcase {index}: Wrong Answer");
                }
                else
                {
                    resultOutput.AppendLine($"Testcase {index}: Accepted");
                }
            }


            if (!hasRuntimeError && submission.Status != "Wrong Answer")
            {
                submission.Status = "Accepted";
            }

            return resultOutput.ToString();
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
