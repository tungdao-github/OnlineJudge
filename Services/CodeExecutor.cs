using Microsoft.AspNetCore.SignalR;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.SignalR;
using System.Diagnostics;
using System.Text;

public class ExecutionResult
{
    public string StandardOutput { get; set; } = "";
    public string StandardError { get; set; } = "";
    public List<TestCaseResult> TestCaseResults { get; set; } = new();
    public long TotalExecutionTimeMs { get; set; }
    public long TotalMemoryUsageBytes { get; set; }
    public string CompilationError { get; set; }
}

public class CodeExecutor
{
    private const int TimeoutMilliseconds = 2000;
    private readonly IHubContext<TestCaseResultHub> _hubContext;

    public CodeExecutor(IHubContext<TestCaseResultHub> hubContext)
    {
        _hubContext = hubContext;
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return string.Join('\n', s.Trim().Replace("\r\n", "\n").Split('\n').Select(x => x.TrimEnd()));
    }

    public async Task<string> CompileCodeAsync(string code, string language)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string exeFile = Path.ChangeExtension(tempFile, ".exe");

        switch (language.ToLower())
        {
            case "cpp":
                tempFile += ".cpp";
                await File.WriteAllTextAsync(tempFile, code);
                return await CompileAsync("g++", $"-O2 -std=c++17 \"{tempFile}\" -o \"{exeFile}\"", tempFile, exeFile);
            case "c":
                tempFile += ".c";
                await File.WriteAllTextAsync(tempFile, code);
                return await CompileAsync("gcc", $"-O2 \"{tempFile}\" -o \"{exeFile}\"", tempFile, exeFile);
            case "python":
                tempFile += ".py";
                await File.WriteAllTextAsync(tempFile, code);
                return tempFile;
            default:
                throw new NotSupportedException($"Language not supported: {language}");
        }
    }

    private async Task<string> CompileAsync(string compiler, string args, string sourceFile, string outputFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = compiler,
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        File.Delete(sourceFile);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Compilation failed:\n{error}");
        }

        return outputFile;
    }

    public async Task<ExecutionResult> RunCodeAsync(string executablePath, List<TestCase> testCases, string language, string connectionId)
    {
        var result = new ExecutionResult();
        var totalStopwatch = Stopwatch.StartNew();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        foreach (var (testCase, i) in testCases.Select((tc, i) => (tc, i)))
        {
            var psi = new ProcessStartInfo
            {
                FileName = (language == "python") ? "python3" : executablePath,
                Arguments = (language == "python") ? $"\"{executablePath}\"" : "",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var testStopwatch = Stopwatch.StartNew();

            try
            {
                process.Start();
                await process.StandardInput.WriteAsync(testCase.Input);
                process.StandardInput.Close();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                var waitTask = process.WaitForExitAsync();
                long memoryUsedBytes = process.PeakWorkingSet64;
                var completed = await Task.WhenAny(waitTask, Task.Delay(TimeoutMilliseconds)) == waitTask;

                if (!completed)
                {
                    Console.WriteLine("tungdao");
                    try { process.Kill(true); } catch { }
                    testStopwatch.Stop();
                    var timeoutResult = new TestCaseResult
                    {
                        TestCaseId = testCase.Id,
                        Input = testCase.Input,
                        ExpectedOutput = Normalize(testCase.ExpectedOutput),
                        ActualOutput = "[TIME LIMIT EXCEEDED]",
                        Passed = false,
                        ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
                        MemoryUsageBytes = memoryUsedBytes
                    };
                    result.TestCaseResults.Add(timeoutResult);
                    //result.CompilationError = "runtime error";
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", timeoutResult);
                    continue;
                }

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                string actualOutput = Normalize(stdout);
                string expectedOutput = Normalize(testCase.ExpectedOutput);
                bool passed = actualOutput == expectedOutput;

                var testCaseResult = new TestCaseResult
                {
                    TestCaseId = testCase.Id,
                    Input = testCase.Input,
                    ExpectedOutput = expectedOutput,
                    ActualOutput = actualOutput,
                    Passed = passed,
                    ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
                    MemoryUsageBytes = memoryUsedBytes
                };

                result.TestCaseResults.Add(testCaseResult);
                outputBuilder.AppendLine(stdout);
                errorBuilder.AppendLine(stderr);

                // Send real-time result via SignalR
                //await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", testCaseResult);
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", testCaseResult);
              
            }
            catch (Exception ex)
            {
                var errorResult = new TestCaseResult
                {
                    TestCaseId = testCase.Id,
                    Input = testCase.Input,
                    ExpectedOutput = Normalize(testCase.ExpectedOutput),
                    ActualOutput = $"[ERROR] {ex.Message}",
                    Passed = false,

                };
                result.TestCaseResults.Add(errorResult);
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", errorResult);
            }
        }

        totalStopwatch.Stop();
        result.TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds;
        result.StandardOutput = outputBuilder.ToString().TrimEnd();
        result.StandardError = errorBuilder.ToString().TrimEnd();
        result.CompilationError = result.StandardError;

        return result;
    }

    public async Task<ExecutionResult> RunAndCompileCodeAsync(string code, List<TestCase> testCases, string language, string connectionId)
    {
        string exePath = null;
        try
        {
            exePath = await CompileCodeAsync(code, language);
        }
        catch (Exception ex)
        {
            return new ExecutionResult { CompilationError = ex.Message };
        }

        var result = await RunCodeAsync(exePath, testCases, language, connectionId);

        if (language != "python")
        {
            try { File.Delete(exePath); } catch { }
        }

        return result;
    }
}
