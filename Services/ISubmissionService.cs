// Services/SubmissionService.cs
using OnlineJudgeAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineJudgeAPI.Controllers;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Models;
namespace OnlineJudge.Services
{
    public interface ISubmissionService
    {
        Task<SubmissionResult> EvaluateSubmission(CodeSubmission submission);
        Task<SubmissionResult> GetResultById(int submissionId);
    }

    public class SubmissionService : ISubmissionService
    {
        private static List<SubmissionResult> _results = new List<SubmissionResult>();
        private static int _nextId = 1;

        public async Task<SubmissionResult> EvaluateSubmission(CodeSubmission submission)
        {
            // Giả lập việc chấm bài (thực tế bạn cần sandbox để chạy mã)
            var result = new SubmissionResult
            {
                Id = _nextId++,
                Status = "Passed", // Giả sử bài nộp luôn pass
                ExecutionTime = 0.2, // Giả lập thời gian thực thi
                MemoryUsed = 1.5, // Giả lập bộ nhớ sử dụng
                Details = "Code executed successfully",
                SubmittedAt = DateTime.UtcNow
            };

            _results.Add(result);
            return await Task.FromResult(result); // Trả về kết quả sau khi chấm bài
        }

        public async Task<SubmissionResult> GetResultById(int submissionId)
        {

            var result = _results.Find(r => r.Id == submissionId);
            return await Task.FromResult(result);
        }
    }
}
