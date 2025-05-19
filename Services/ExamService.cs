using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;

public class ExamService {
    private readonly ApplicationDbContext _context;
    public ExamService(ApplicationDbContext context) {
        _context = context;
    }
    public async Task<List<ExamResultStudent>> CalculateExamRoomResultsAsync(int examRoomId)
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

            // Group by ProblemId and take highest score per problem
            var totalScore = studentSubmissions
                .GroupBy(s => s.ProblemId)
                .Select(g => g.Max(s => s.Score ?? 0))
                .Sum();

            results.Add(new ExamResultStudent
            {
                ExamRoomId = examRoomId,
                ExamPaperId = student.ExamPaperId,
                UserId = student.UserId,
                TotalScore = totalScore,
                CalculatedAt = DateTime.UtcNow
            });
        }

        // Lưu kết quả
        _context.ExamResultStudents.AddRange(results);
        await _context.SaveChangesAsync();

        return results;
    }
}