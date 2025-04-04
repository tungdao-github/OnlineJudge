namespace OnlineJudgeAPI.Services
{
    public interface INotificationService
    {
        Task SendSubmissionUpdate(Submission submission);
    }
}