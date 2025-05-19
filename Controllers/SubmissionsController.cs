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

    public class ExamResult
    {
        public int Id { get; set; }
    
        public string masv{get; set;}
        public int StudentId { get; set; }      // Khóa ngoại tới User
        public string FullName { get; set; }    // Họ và tên sinh viên
        // public int SubmissionId { get; set; }   // Khóa ngoại tới Submission
        public string Subject { get; set; }     // Ví dụ: "Phát triển ứng dụng di động"
        public int? Score { get; set; }       // Điểm
        public DateTime SubmittedAt { get; set; }
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
                    UserId = userId,
                    ContestId = contestId,
                    ExamRoomId = submissionRequest.examRoomId
                };
                _context.Submissions.Add(submission);
                var testCases = await _context.TestCases.Where(t => t.ProblemId== submissionRequest.ProblemId).ToListAsync();
                var results = await ProcessTestCasesAsync(testCases, submission, submissionRequest.ConnectionId, problem.maxScore);
              

                submission.Result = _codeExecutor.Results;
                submission.Score = _codeExecutor.Score;
            // submission.Score = results.Score;

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
                else {
    //             var examStudent = await _context.ExamStudents
    // .Include(es => es.ExamPaper)
    //     .ThenInclude(ep => ep.Problems)
    // .FirstOrDefaultAsync(es => es.UserId == userId);
    //             var totalScore = await CalculateTotalScoreForStudent(userId);
    //             var examResult = new ExamResult
    //             {
    //                 StudentId = userId,
    //                 FullName = examStudent.FullName,
    //             // hoặc bỏ nếu bạn không dùng submission cụ thể
    //                 Subject = examStudent.ExamRoom.SubjectName,
    //                 Score = totalScore,
    //                 SubmittedAt = DateTime.Now
    //             };

    //             _context.ExamResults.Add(examResult);
    //             await _context.SaveChangesAsync();
            }
                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group($"contest_{contestId}")
                    .SendAsync("UpdateStandings", contestId);

                return Ok(new { message = "Submission received!", submissionId = submission.Id });
            }
            
           

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
                    Score = _codeExecutor.Score,
                    IsCorrect = "false"
                };
            }
            int passedCount = 0;
            int lineIndex = 0;



                int score = (int)Math.Round(100.0 * passedCount / testCases.Count);

                submission.IsCorrect = passedCount == testCases.Count && ok ? "true" : "false";
                submission.Status = passedCount == testCases.Count && ok? "Accepted" : "Wrong Answer";
                submission.Status = _codeExecutor.status == true ? "Accepted" : "Wrong Answer"; 
                submission.Score = _codeExecutor.Score;
            return new submit {
                ExecutionTimeMs = result.TotalExecutionTimeMs,
                MemoryUsageBytes = result.TotalMemoryUsageBytes,
                Error = "",
                Result = _codeExecutor.Results,
                Score = _codeExecutor.Score,
                IsCorrect = submission.IsCorrect,
                PassedTestCases = passedCount
            };

        }

        // public async Task<int?> CalculateTotalScoreForStudent(int userId)
        // {
        //     // Lấy thông tin sinh viên trong phòng thi
        //     var examStudent = await _context.ExamStudents
        //         .Include(es => es.ExamPaper)
        //             .ThenInclude(ep => ep.Problems)
        //         .FirstOrDefaultAsync(es => es.UserId == userId);

        //     if (examStudent == null)
        //         throw new Exception("Sinh viên không có trong phòng thi.");

        //     // Lấy danh sách các bài trong đề thi
        //     var allowedProblemIds = examStudent.ExamPaper.Problems
        //         .Select(p => p.ProblemId)
        //         .ToHashSet();

        //     // Lấy tất cả bài nộp của sinh viên, lọc ra những bài có trong đề
        //     var validSubmissions = await _context.Submissions
        //         .Where(s => s.UserId == userId && allowedProblemIds.Contains(s.ProblemId))
        //         .GroupBy(s => s.ProblemId)
        //         .Select(g => g.OrderByDescending(s => s.Score).First()) // lấy điểm cao nhất mỗi bài
        //         .ToListAsync();

        //     // Tính tổng điểm
        //     int? totalScore = validSubmissions.Sum(s => s.Score);
        //     return totalScore;
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
        public int? examRoomId {get; set;}
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
