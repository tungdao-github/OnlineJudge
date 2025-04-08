namespace OnlineJudgeAPI.Services
{
    public interface INotificationService
    {
        Task SendSubmissionUpdate(Submission submission);

        //Task SendTestCaseResult(int submissionId, int testCaseId, string message);
    }
}