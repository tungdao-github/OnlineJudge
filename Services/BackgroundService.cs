using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace OnlineJudgeAPI.Services
{
    public class SubmissionQueue
    {
        private readonly ConcurrentQueue<int> _queue = new();
        public void Enqueue(int submissionId) => _queue.Enqueue(submissionId);
        public bool TryDequeue(out int submissionId) => _queue.TryDequeue(out submissionId);
    }
    public class SubmissionProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SubmissionQueue _submissionQueue;
        private readonly CodeExecutor _codeExecutor;

        public SubmissionProcessingService(IServiceScopeFactory scopeFactory, SubmissionQueue submissionQueue, CodeExecutor codeExecutor)
        {
            _scopeFactory = scopeFactory;
            _submissionQueue = submissionQueue;
            _codeExecutor = codeExecutor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_submissionQueue.TryDequeue(out int submissionId))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var submission = await context.Submissions.FindAsync(submissionId);
                    if (submission == null) continue;

                    var testCases = await context.TestCases
                        .Where(tc => tc.ProblemId == submission.ProblemId)
                        .ToListAsync();

                    string resultSummary = "";
                    bool hasError = false;

                    foreach (var testCase in testCases)
                    {
                        var (output, error) = await _codeExecutor.RunCodeAsync(submission.Language, submission.Code, testCase.Input);
                        if (!string.IsNullOrEmpty(error))
                        {
                            submission.Status = "Runtime Error";
                            submission.Error = error;
                            hasError = true;
                            break;
                        }

                        if (output.Trim() != testCase.ExpectedOutput.Trim())
                        {
                            resultSummary += $"Test case {testCase.Id}: Wrong Answer\n";
                            submission.Status = "Wrong Answer";
                        }
                        else
                        {
                            resultSummary += $"Test case {testCase.Id}: Accepted\n";
                        }
                    }

                    if (!hasError && submission.Status == "Pending")
                    {
                        submission.Status = "Accepted";
                    }

                    submission.Result = resultSummary;
                    await context.SaveChangesAsync();
                }

                await Task.Delay(100); // Tránh sử dụng CPU quá mức
            }
        }
    }

}
