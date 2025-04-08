//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using OnlineJudgeAPI.Services;
//using OnlineJudgeAPI.Models;
////using OnlineJudge.Services;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using System.Text;
//using System.Collections.Concurrent;
//using Microsoft.AspNetCore.Authorization;
//using System.Security.Claims;
//using System.Diagnostics;
//using Microsoft.EntityFrameworkCore.Storage;

//namespace OnlineJudgeAPI.Controllers
//{
   

//    [Route("api/[controller]")]
//    [ApiController]
//    public class SubmissionsController2 : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly CodeExecutor _codeExecutor;
//        private readonly ISubmissionService _submissionService;
//        private readonly SubmissionQueue _submissionQueue;
//        private readonly ICacheService _cache;
//        private readonly INotificationService _notificationService;

//        public SubmissionsController2(
//            ApplicationDbContext context,
//            CodeExecutor codeExecutor,
//            ISubmissionService submissionService,
//            SubmissionQueue submissionQueue,
//            ICacheService cache,
//            INotificationService notificationService)
//        {
//            _context = context;
//            _codeExecutor = codeExecutor;
//            _submissionService = submissionService;
//            _submissionQueue = submissionQueue;
//            _cache = cache;
//            _notificationService = notificationService;
//        }

//        //[HttpGet("GetResult/{id}")]
//        //public async Task<ActionResult<SubmissionResult>> GetResult(int id)
//        //{
//        //    var submission = await _context.Submissions
//        //                       .AsNoTracking()
//        //                       .FirstOrDefaultAsync(s => s.Id == id);
//        //    if (submission == null)
//        //    {
//        //        return NotFound("Submission not found.");
//        //    }

//        //    var result = await _submissionService.GetResultById(id);
//        //    return Ok(result ?? new SubmissionResult { Status = "Not Available" });
//        //}
//        [HttpGet("GetResult/{id}")]
//        public async Task<ActionResult<SubmissionResult>> GetResult(int id)
//        {
//            var result = await _context.Submissions
//                                       .AsNoTracking()
//                                       .Where(s => s.Id == id)
//                                       .Select(s => new SubmissionResult
//                                       {
//                                           Status = s.Status,
//                                           result = s.Result
//                                       })
//                                       .FirstOrDefaultAsync();

//            if (result == null)
//                return NotFound(new { message = "Submission not found" });

//            if (string.IsNullOrEmpty(result.result) || result.Status == "Pending" || result.Status == "Processing")
//            {
//                return Ok(new { message = "Submission is still being processed", submissionId = id });
//            }

//            return Ok(result);
//        }
//        //[Authorize(Roles = "User,Admin")]
//        [HttpPost("submit")]
//        public async Task<ActionResult<Submission>> SubmitCode([FromBody] SubmissionRequest request)
//        {
//            if (!ModelState.IsValid)
//            {
//                return BadRequest(ModelState);
//            }

//            var submission = new Submission
//            {
//                ProblemId = request.ProblemId,
//                Code = request.Code,
//                Language = request.Language,
//                Status = "Pending",
//                Error = string.Empty,
//                Result = "Processing"
//            };

//            _context.Submissions.Add(submission);
//            await _context.SaveChangesAsync();

//            // Cache test cases for the problem
//            var cacheKey = $"testcases:{submission.ProblemId}";
//            var testCases = await _cache.GetAsync<List<TestCase>>(cacheKey) ??
//                            await _context.TestCases.Where(tc => tc.ProblemId == submission.ProblemId).ToListAsync();

//            await _cache.SetAsync(cacheKey, testCases, TimeSpan.FromMinutes(10));

//            // Process test cases concurrently but with limited parallelism
//            var results = await ProcessTestCasesAsync(testCases, submission);

//            // Update submission result
//            submission.Result = results;
//            await _context.SaveChangesAsync();

//            // Send real-time notification
//            await _notificationService.SendSubmissionUpdate(submission);

//            return Ok(new { message = "Submission received!", submissionId = submission.Id });
//        }

//        private async Task<string> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission)
//        {
//            var results = new ConcurrentDictionary<int, ExecutionResult>();
//            var maxConcurrency = 150; // Giảm từ 4 xuống 2 để kiểm tra
//            var semaphore = new SemaphoreSlim(maxConcurrency);

//            var tasks = testCases.Select(async testCase =>
//            {
//                await semaphore.WaitAsync();
//                try
//                {
//                    Console.WriteLine($"[DEBUG] Processing TestCase {testCase.Id}  {testCase.Input} {testCase.ExpectedOutput}...");
                    
//                    var result = await _codeExecutor.RunCodeAsync(submission.Code, testCases, submission.Language);
//                    Console.WriteLine("output, error = " + result.StandardOutput + " " + result.StandardError);
//                    //Console.WriteLine("ket qua cua executionResult.StandardOutput.Trim() != expectedOutput.Trim() = " + result.StandardOutput.Trim() != result.);
//                    results[testCase.Id] = result;
//                }
//                finally
//                {
//                    semaphore.Release();
//                }
//            });

//            await Task.WhenAll(tasks);

//            var resultOutput = new StringBuilder();
//            bool hasRuntimeError = false;

//            foreach (var (index, executionResult) in results.OrderBy(r => r.Key))
//            {
//                var expectedOutput = testCases.First(tc => tc.Id == index).ExpectedOutput.Trim();
//                Console.WriteLine($"[DEBUG] TestCase {index}: Expected [{expectedOutput}], Got [{executionResult.StandardOutput.Trim()}]");
//                Console.WriteLine("ket qua cua executionResult.StandardOutput.Trim() != expectedOutput.Trim() = " + executionResult.StandardOutput.Trim() != expectedOutput);
//                if (!string.IsNullOrEmpty(executionResult.StandardError))
//                {
//                    submission.Status = "Runtime Error";
//                    submission.Error = executionResult.StandardError;
//                    resultOutput.AppendLine($"{executionResult.StandardError}\nTestcase {index}: Runtime Error");
//                    hasRuntimeError = true;
//                    break;
//                }
                
//                if (executionResult.StandardOutput.Trim() != expectedOutput.Trim())
//                {
//                    submission.Status = "Wrong Answer";
//                    resultOutput.AppendLine($"Testcase {index}: Wrong Answer");
//                }
//                else
//                {
//                    resultOutput.AppendLine($"Testcase {index}: Accepted");
//                }
//            }


//            if (!hasRuntimeError && submission.Status != "Wrong Answer")
//            {
//                submission.Status = "Accepted";
//            }

//            return resultOutput.ToString();
//        }
//        //private async Task<string> ProcessTestCasesAsync(List<TestCase> testCases, Submission submission)
//        //{
//        //    var allInputs = string.Join('\n', testCases.Select(tc => tc.Input.Trim()));
//        //    var stopwatch = Stopwatch.StartNew();
//        //    var executionResult = await _codeExecutor.RunCodeAsync(submission.Language, submission.Code, allInputs);
//        //    stopwatch.Stop();

//        //    var resultOutput = new StringBuilder(testCases.Count * 100); // pre-allocate space

//        //    // Check for runtime error once
//        //    if (!string.IsNullOrWhiteSpace(executionResult.error))
//        //    {
//        //        submission.Status = "Runtime Error";
//        //        submission.Error = executionResult.error;

//        //        foreach (var testCase in testCases)
//        //        {
//        //            resultOutput.AppendLine($"Testcase {testCase.Id}: Runtime Error");
//        //            resultOutput.AppendLine($"Error: {executionResult.error}");
//        //            resultOutput.AppendLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
//        //            resultOutput.AppendLine($"Memory: {executionResult.memoryUsage} bytes");
//        //        }

//        //        return resultOutput.ToString();
//        //    }

//        //    // Split all outputs once
//        //    var outputs = executionResult.output.Split('\n', StringSplitOptions.TrimEntries);

//        //    bool hasWrongAnswer = false;

//        //    for (int i = 0; i < testCases.Count; i++)
//        //    {
//        //        var expected = testCases[i].ExpectedOutput.Trim();
//        //        var actual = i < outputs.Length ? outputs[i] : string.Empty;

//        //        if (expected != actual)
//        //        {
//        //            resultOutput.AppendLine($"Testcase {testCases[i].Id}: Wrong Answer");
//        //            resultOutput.AppendLine($"Expected: {expected}");
//        //            resultOutput.AppendLine($"Got: {actual}");
//        //            resultOutput.AppendLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
//        //            resultOutput.AppendLine($"Memory: {executionResult.memoryUsage} bytes");
//        //            hasWrongAnswer = true;
//        //        }
//        //        else
//        //        {
//        //            resultOutput.AppendLine($"Testcase {testCases[i].Id}: Accepted");
//        //            resultOutput.AppendLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
//        //            resultOutput.AppendLine($"Memory: {executionResult.memoryUsage} bytes");
//        //        }
//        //    }

//        //    if (hasWrongAnswer)
//        //    {
//        //        submission.Status = "Wrong Answer";
//        //    }
//        //    else
//        //    {
//        //        submission.Status = "Accepted";
//        //    }

//        //    return resultOutput.ToString();
//        //}

//        [HttpGet("list")]
//        public async Task<IActionResult> GetSubmissions()
//        {
//            var submissions = await _context.Submissions.AsNoTracking().ToListAsync();
//            return Ok(submissions);
//        }

//        [HttpGet("{id}")]
//        public async Task<ActionResult<Submission>> GetSubmission(int id)
//        {
//            var submission = await _context.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
//            return submission ?? (ActionResult<Submission>)NotFound();
//        }

//        [HttpPost("judge/{id}")]
//        public async Task<IActionResult> JudgeSubmission(int id, [FromBody] JudgeRequest request)
//        {
//            var submission = await _context.Submissions.FindAsync(id);
//            if (submission == null)
//                return NotFound(new { message = "Submission not found" });

//            submission.Status = request.Status;
//            await _context.SaveChangesAsync();
//            return Ok(new { message = "Submission judged", status = submission.Status });
//        }
//    }

//    // Models
 
//}
