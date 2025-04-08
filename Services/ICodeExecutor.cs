
using OnlineJudgeAPI.Models;

public interface ICodeExecutor
{
    Task<SubmissionResult> ExecuteAsync(string code, string input, string language);
}
