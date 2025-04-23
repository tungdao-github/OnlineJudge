// CodeExecutor.cs
using OnlineJudgeAPI.Models;
using System.Diagnostics;
using System.Text;

public class CodeExecutor2 : ICodeExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(string code, List<TestCase> testCases)
    {
        var fileName = $"solution_{Guid.NewGuid().ToString("N")}.cpp";
        await File.WriteAllTextAsync(fileName, code);

        var exeName = fileName.Replace(".cpp", ".exe");

        var compileProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "g++",
            Arguments = $"-O2 -std=c++17 {fileName} -o {exeName}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        await compileProcess.WaitForExitAsync();

        if (compileProcess.ExitCode != 0)
        {
            var error = await compileProcess.StandardError.ReadToEndAsync();
            return new ExecutionResult { CompilationError = error };
        }

        var testResults = new List<TestCaseResult>();
        foreach (var test in testCases)
        {
            var result = await RunTestCaseAsync(exeName, test);
            testResults.Add(result);
        }

        File.Delete(fileName);
        File.Delete(exeName);
        return null;
        //return new ExecutionResult { TestCases = testResults };
    }

    public Task<SubmissionResult> ExecuteAsync(string code, string input, string language)
    {
        throw new NotImplementedException();
    }

    private async Task<TestCaseResult> RunTestCaseAsync(string exePath, TestCase test)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        var stopwatch = Stopwatch.StartNew();
        process.Start();

        await process.StandardInput.WriteAsync(test.Input);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        stopwatch.Stop();

        return new TestCaseResult
        {
            Input = test.Input,
            ExpectedOutput = test.ExpectedOutput,
            ActualOutput = output.Trim(),
            Passed = output.Trim() == test.ExpectedOutput.Trim(),
            Error = error,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }
}

//public class ExecutionResult
//{
//    public string CompilationError { get; set; }
//    public List<TestCaseResult> TestCases { get; set; } = new();
//}

public class TestCaseResult
{
    public int TestCaseId;
    public string Input { get; set; }
    public string ExpectedOutput { get; set; }
    public string ActualOutput { get; set; }
    public bool Passed { get; set; }
    public string Error { get; set; }
    public double ExecutionTimeMs { get; set; }
    public long MemoryUsageBytes { get; set; }
}