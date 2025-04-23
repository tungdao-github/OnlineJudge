
using OnlineJudgeAPI.Models;
//using OnlineJudgeAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

public class SubmissionService : ISubmissionService
{
    //private readonly ITestCaseService _testCaseService;
    //private readonly ICodeExecutor _codeExecutor;
    //private readonly IHubContext<NotificationHub> _hubContext;

    //public SubmissionService(ITestCaseService testCaseService, ICodeExecutor codeExecutor, IHubContext<NotificationHub> hubContext)
    //{
    //    _testCaseService = testCaseService;
    //    _codeExecutor = codeExecutor;
    //    _hubContext = hubContext;
    //}
     
    public async Task<List<SubmissionResult>> SubmitCodeAsync(string code, int problemId, string language, string connectionId, int submissionId)
    {
        //    var testCases = await _testCaseService.GetTestCasesForProblemAsync(problemId);
        //    var results = new List<SubmissionResult>();

        //    foreach (var testCase in testCases)
        //    {
        //        var result = await _codeExecutor.ExecuteAsync(code, testCase.Input, language);
        //        result.Verdict = result.Output.Trim() == testCase.ExpectedOutput.Trim() ? "Accepted" : "Wrong Answer";

        //        results.Add(result);

        //        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", new
        //        {
        //            SubmissionId = submissionId,
        //            Input = testCase.Input,
        //            ExpectedOutput = testCase.ExpectedOutput,
        //            Output = result.Output,
        //            Verdict = result.Verdict,
        //            Time = result.ExecutionTimeMs,
        //            Memory = result.MemoryUsageBytes
        //        });
        //    }

            return null;
    }
}
