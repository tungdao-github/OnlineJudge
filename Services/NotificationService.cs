using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using OnlineJudgeAPI.Models;

namespace OnlineJudgeAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task SendSubmissionUpdate(Submission submission)
        {
            _logger.LogInformation($"Submission {submission.Id} status updated to: {submission.Status}");
            await Task.CompletedTask;
        }
    }
}
