
using OnlineJudgeAPI.Models;

public interface ICodeExecutor
{
    public  Task<ExecutionResult> RunCodeAsync(string executablePath, List<TestCase> testCases, string language, string connectionId, int maxScore);
    public  Task<ExecutionResult> RunAndCompileCodeAsync(string code, List<TestCase> testCases, string language, string connectionId, int maxScore);
}
