// Các thực thể mở rộng
using OnlineJudgeAPI.Models;
using Microsoft.AspNetCore.Mvc;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.Services;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;



public class CreateExamRoomRequest
{
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public string SubjectName { get; set; }
    public string SubjectCode { get; set; }
    public DateTime ExamDate { get; set; }
    public string ExamTime { get; set; }
    public string ExamType { get; set; }
    public string ClassCode { get; set; }
    public string Attempt { get; set; }
    public string ExamId { get; set; }
    public DateTime StartTime { get; set; }
}

public class CreateExamPapersRequest
{
    public int ExamRoomId { get; set; }
    public List<ExamPaperWithProblems> Papers { get; set; }
}

public class ExamPaperWithProblems
{
    public string Code { get; set; }
    public List<int> ProblemIds { get; set; }
}

public class AddStudentsToRoomRequest
{
    public int ExamRoomId { get; set; }
    public List<StudentAssignment> Students { get; set; }
}

public class StudentAssignment
{
    public int UserId { get; set; }
    public string SeatCode { get; set; }
    public string PaperCode { get; set; }
    public string FullName { get; set; }
    public string IdentityCard { get; set; }
    public string ExamCode { get; set; }
    public string FeeStatus { get; set; }
}

public class ExamRoomDetailDto
{
    public int Id { get; set; }
    public string RoomName { get; set; }
    public DateTime StartTime { get; set; }
    public List<ExamPaperDto> ExamPapers { get; set; }
    public List<ExamStudentDto> Students { get; set; }
}

public class ExamPaperDto
{
    public int Id { get; set; }
    public string Code { get; set; }
    public List<ExamPaperProblemDto> Problems { get; set; }
}

public class ExamPaperProblemDto
{
    public int ProblemId { get; set; }
    public string DisplayOrder { get; set; }
}

public class ExamStudentDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string IdentityCard { get; set; }
    public string SeatCode { get; set; }
    public string ExamCode { get; set; }
    public string FeeStatus { get; set; }
    public string PaperCode { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class ExamRoomController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    // private readonly ExamService _examService;
    public ExamRoomController(ApplicationDbContext context)
    {
        _context = context;
        // _examService = examService;
    }

    [HttpPost("create-room")]
    public async Task<IActionResult> CreateRoom(CreateExamRoomRequest request)
    {
        var room = new ExamRoom
        {
            RoomCode = request.RoomCode,
            RoomName = request.RoomName,
            SubjectName = request.SubjectName,
            SubjectCode = request.SubjectCode,
            ExamDate = request.ExamDate,
            ExamTime = request.ExamTime,
            ExamType = request.ExamType,
            ClassCode = request.ClassCode,
            Attempt = request.Attempt,
            ExamId = request.ExamId,
            StartTime = request.StartTime
        };

        _context.ExamRooms.Add(room);
        await _context.SaveChangesAsync();
        return Ok(room);
    }
    [HttpPost("add-papers")]
    public async Task<IActionResult> AddExamPapers(CreateExamPapersRequest request)
    {
        Console.WriteLine("request.ExamRoomId) = " + request.ExamRoomId);
        var room = await _context.ExamRooms.FindAsync(request.ExamRoomId);
        if (room == null) return NotFound("Room not found");

        foreach (var paperDto in request.Papers)
        {
            var paper = new ExamPaper
            {
                ExamRoomId = room.Id,
                Code = paperDto.Code,
                Problems = new List<ExamPaperProblem>()
            };
            _context.ExamPapers.Add(paper);
            await _context.SaveChangesAsync();

            int order = 1;
            foreach (var pid in paperDto.ProblemIds)
            {
                _context.ExamPaperProblems.Add(new ExamPaperProblem
                {
                    ExamPaperId = paper.Id,
                    ProblemId = pid,
                    DisplayOrder = order.ToString()
                });
                order++;
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("add-students")]
    public async Task<IActionResult> AddStudentsToRoom(AddStudentsToRoomRequest request)
    {
        var room = await _context.ExamRooms.FindAsync(request.ExamRoomId);
        if (room == null) return NotFound("Room not found");

        // Lấy tất cả mã đề trong phòng thi này (Code + ExamRoomId duy nhất)
        var paperDict = await _context.ExamPapers
            .Where(p => p.ExamRoomId == room.Id)
            .ToDictionaryAsync(p => p.Code!, p => p);

        foreach (var student in request.Students)
        {
            // Kiểm tra mã đề có tồn tại trong phòng thi
            if (!paperDict.TryGetValue(student.PaperCode, out var paper))
            {
                return BadRequest($"Paper code '{student.PaperCode}' not found in room {room.Id}");
            }

            // Gán sinh viên vào phòng thi với mã đề tương ứng
            var examStudent = new ExamStudent
            {
                ExamRoomId = room.Id,
                UserId = student.UserId,
                ExamPaperId = paper.Id, // đúng theo ExamRoomId
                SeatCode = student.SeatCode,
                FullName = student.FullName,
                IdentityCard = student.IdentityCard,
                ExamCode = student.ExamCode,
                FeeStatus = student.FeeStatus
            };
            _context.ExamStudents.Add(examStudent);
        }

        await _context.SaveChangesAsync();
        return Ok("Students added successfully");
    }


   
    [HttpGet]
    public async Task<IActionResult> GetAllExamRooms()
    {
        var rooms = await _context.ExamRooms
            .Select(r => new
            {
                r.Id,
                r.RoomCode,
                r.RoomName,
                r.SubjectName,
                r.SubjectCode,
                r.ExamDate,
                r.ExamTime,
                r.ExamType,
                r.ClassCode,
                r.Attempt,
                r.ExamId,
                r.StartTime
            }).ToListAsync();

        return Ok(rooms);
    }
    [HttpGet("rooms/{roomId}/problems")]
    public async Task<IActionResult> GetProblemsInRoom(int roomId)
    {
        var problems = await _context.ExamPapers
            .Where(p => p.ExamRoomId == roomId)
            .SelectMany(p => p.Problems)
            .Select(epp => new
            {
                epp.Problem.Id,
                epp.Problem.Title,
                epp.Problem.Description,
                epp.Problem.maxScore
            })
            .Distinct()
            .ToListAsync();

        return Ok(problems);
    }

    [HttpGet("students/problems")]
    public IActionResult GetStudentProblems()
    {
        var result = _context.ExamStudents
            .Include(es => es.ExamPaper)
                .ThenInclude(ep => ep.Problems)
                    .ThenInclude(epp => epp.Problem)
            .Select(es => new
            {
                StudentId = es.UserId,
                FullName = es.FullName,
                ExamPaperCode = es.ExamPaper.Code,
                Problems = es.ExamPaper.Problems.Select(p => new
                {
                    ProblemId = p.Problem.Id,
                    Title = p.Problem.Title,
                    Description = p.Problem.Description
                }).ToList()
            })
            .ToList();

        return Ok(result);
    }


    
    [HttpPost("{examRoomId}/calculate-results")]
    public async Task<IActionResult> CalculateResults(int examRoomId)
    {
        var examStudents = await _context.ExamStudents
          .Include(es => es.ExamPaper)
              .ThenInclude(p => p.Problems)
          .Where(es => es.ExamRoomId == examRoomId)
          .ToListAsync();

        var results = new List<ExamResultStudent>();

        foreach (var student in examStudents)
        {
            var expectedProblemIds = student.ExamPaper.Problems.Select(p => p.ProblemId).ToList();

            var studentSubmissions = await _context.Submissions
                .Where(s => s.UserId == student.UserId
                            && s.ExamRoomId == examRoomId
                            && expectedProblemIds.Contains(s.ProblemId))
                .ToListAsync();

            var totalScore = studentSubmissions
                .GroupBy(s => s.ProblemId)
                .Select(g => g.Max(s => s.Score ?? 0))
                .Sum();

            // Tìm kết quả đã có (nếu có)
            var existing = await _context.ExamResultStudents.FirstOrDefaultAsync(
                r => r.ExamRoomId == examRoomId && r.UserId == student.UserId);

            if (existing != null)
            {
                existing.TotalScore = totalScore;
                existing.CalculatedAt = DateTime.UtcNow;
                existing.ExamPaperId = student.ExamPaperId;
                _context.ExamResultStudents.Update(existing);
            }
            else
            {
                results.Add(new ExamResultStudent
                {
                    ExamRoomId = examRoomId,
                    ExamPaperId = student.ExamPaperId,
                    UserId = student.UserId,
                    TotalScore = totalScore,
                    CalculatedAt = DateTime.UtcNow
                });
            }
        }

        // Thêm mới các record chưa tồn tại
        if (results.Any())
        {
            _context.ExamResultStudents.AddRange(results);
        }

        await _context.SaveChangesAsync();
        return Ok(await _context.ExamResultStudents
            .Where(r => r.ExamRoomId == examRoomId)
            .ToListAsync());
    }
    [Authorize]
    [HttpGet("my-problems")]
    public IActionResult GetMyProblems()
    {
        try
        {
            // Lấy userId từ JWT claims
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
            }

            int userId = int.Parse(userIdClaim.Value);

            // Tìm bản ghi ExamStudent tương ứng
            var examStudent = _context.ExamStudents
                .Include(es => es.ExamPaper)
                    .ThenInclude(ep => ep.Problems)
                        .ThenInclude(epp => epp.Problem)
                .FirstOrDefault(es => es.UserId == userId);

            if (examStudent == null)
            {
                return NotFound(new { message = "Bạn chưa được phân vào mã đề nào." });
            }

            // Nếu không có ExamPaper
            if (examStudent.ExamPaper == null)
            {
                return NotFound(new { message = "Không tìm thấy mã đề thi của bạn." });
            }

            // Lấy danh sách bài tập từ ExamPaper
            var problems = examStudent.ExamPaper.Problems
                .Select(p => new
                {
                    problemId = p.Problem.Id,
                    title = p.Problem.Title,
                    description = p.Problem.Description
                })
                .ToList();

            return Ok(new
            {
                fullName = examStudent.FullName,
                examPaperCode = examStudent.ExamPaper.Code,
                problems = problems
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
        }
    }

}
