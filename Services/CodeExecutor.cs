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
    private const int TimeoutMilliseconds = 3000;
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

        var tasks = testCases.Select(testCase =>
            RunSingleTestCaseAsync(testCase, executablePath, language, connectionId)).ToList();

        var results = await Task.WhenAll(tasks);

        result.TestCaseResults.AddRange(results);
        result.TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds;

        result.StandardOutput = string.Join("\n", results.Select(r => r.ActualOutput));
        result.StandardError = string.Join("\n", results.Where(r => !r.Passed).Select(r => r.ActualOutput.Contains("[ERROR]") ? r.ActualOutput : "").Where(x => !string.IsNullOrWhiteSpace(x)));

        return result;
    }
    public async Task<ExecutionResult> RunAsync(string sourceCode, string language, string input)
    {
        var result = new ExecutionResult();

        // 1. Tạo file tạm
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        string filePath = Path.Combine(tempDir, GetFileName(language));
        await File.WriteAllTextAsync(filePath, sourceCode);

        string exePath = string.Empty;
        string compileCmd = string.Empty;
        string runCmd = string.Empty;

        // 2. Compile + Run command
        switch (language.ToLower())
        {
            case "cpp":
                exePath = Path.Combine(tempDir, "a.exe");
                compileCmd = $"g++ \"{filePath}\" -o \"{exePath}\"";
                runCmd = $"\"{exePath}\"";
                break;
            case "python":
                runCmd = $"python \"{filePath}\"";
                break;
            case "java":
                compileCmd = $"javac \"{filePath}\"";
                runCmd = $"java -cp \"{tempDir}\" Main";
                break;
            default:
                result.StandardOutput  = "";
                result.StandardError = "Unsupported language.";
                return result;
        }

        // 3. Compile (nếu có)
        if (!string.IsNullOrWhiteSpace(compileCmd))
        {
            var compile = await RunProcess(compileCmd);
            if (!string.IsNullOrWhiteSpace(compile.stderr))
            {
                result.CompilationError = compile.stderr;
                return result;
            }
        }

        // 4. Run with input
        var runResult = await RunProcess(runCmd, input);

        result.StandardOutput = runResult.stdout;
        result.TotalExecutionTimeMs = runResult.timeUsed;
        result.TotalMemoryUsageBytes = runResult.memoryUsed;
        result.CompilationError = runResult.stderr;

        // 5. Cleanup
        try { Directory.Delete(tempDir, true); } catch { }

        return result;
    }

    private string GetFileName(string language)
    {
        return language.ToLower() switch
        {
            "cpp" => "main.cpp",
            "python" => "main.py",
            "java" => "Main.java",
            _ => "main.txt"
        };
    }

    private async Task<(string stdout, string stderr, long timeUsed, long memoryUsed)> RunProcess(string cmd, string input = "")
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {cmd}",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stopwatch = new Stopwatch();
        process.Start();

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        stopwatch.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        stopwatch.Stop();

        return (output.Trim(), error.Trim(), 0, 0); // memoryUsed = 0 (placeholder)
    }
    public async Task<SubmissionResult2> RunCodeAndCompare(Submission submission, TestCase testCase)
    {
        var result = new SubmissionResult2();

        var execution = await RunAsync(
            submission.Code,
            submission.Language,
            testCase.Input
        );

        result.ExecutionTime = execution.TotalExecutionTimeMs;
        result.MemoryUsed = execution.TotalMemoryUsageBytes;
        result.output = execution.StandardOutput?.Trim();
        result.ExpectedOutput = testCase.ExpectedOutput?.Trim();
        result.error = execution.CompilationError;

        if (!string.IsNullOrWhiteSpace(execution.CompilationError))
        {
            result.IsCorrect = false;
            result.Status = execution.CompilationError.Contains("error") ? "Compilation Error" : "Runtime Error";
        }
        else if (Normalize(result.ActualOutput) == Normalize(result.ExpectedOutput))
        {
            result.IsCorrect = true;
            result.Status = "Accepted";
        }
        else
        {
            result.IsCorrect = false;
            result.Status = "Wrong Answer";
        }

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
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", new ExecutionResult { CompilationError = ex.Message });

            return new ExecutionResult { CompilationError = ex.Message };

        }

        var result = await RunCodeAsync(exePath, testCases, language, connectionId);

        if (language != "python")
        {
            try { File.Delete(exePath); } catch { }
        }

        return result;
    }

    private async Task<TestCaseResult> RunSingleTestCaseAsync(TestCase testCase, string executablePath, string language, string connectionId)
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

    using var process = new Process { StartInfo = psi, EnableRaisingEvents = false };
    var testStopwatch = Stopwatch.StartNew();

    try
    {
        process.Start();

        await process.StandardInput.WriteAsync(testCase.Input);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();

        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeoutMilliseconds)) == waitTask;

        testStopwatch.Stop();

        if (!completed)
        {
            try { process.Kill(true); } catch { }

            var timeoutResult = new TestCaseResult
            {
                TestCaseId = testCase.Id,
                Input = testCase.Input,
                ExpectedOutput = Normalize(testCase.ExpectedOutput),
                ActualOutput = "[RUN Time Error]",
                Passed = false,
                ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
                MemoryUsageBytes = 0
            };
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", timeoutResult);
            return timeoutResult;
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        long memoryUsedBytes = 0;
        try { memoryUsedBytes = process.PeakWorkingSet64; } catch { }

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

        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", testCaseResult);
        return testCaseResult;
    }
    catch (Exception ex)
    {
        testStopwatch.Stop();
        var errorResult = new TestCaseResult
        {
            TestCaseId = testCase.Id,
            Input = testCase.Input,
            ExpectedOutput = Normalize(testCase.ExpectedOutput),
            ActualOutput = $"[ERROR] {ex.Message}",
            Passed = false,
            ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
            MemoryUsageBytes = 0
        };

        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", errorResult);
        return errorResult;
    }
}

    //private async Task<TestCaseResult> RunSingleTestCaseAsync(TestCase testCase, string executablePath, string language, string connectionId)
    //{
    //    var psi = new ProcessStartInfo
    //    {
    //        FileName = (language == "python") ? "python3" : executablePath,
    //        Arguments = (language == "python") ? $"\"{executablePath}\"" : "",
    //        RedirectStandardInput = true,
    //        RedirectStandardOutput = true,
    //        RedirectStandardError = true,
    //        UseShellExecute = false,
    //        CreateNoWindow = true,
    //        StandardOutputEncoding = Encoding.UTF8,
    //        StandardErrorEncoding = Encoding.UTF8
    //    };

    //    using var process = new Process { StartInfo = psi };
    //    var testStopwatch = Stopwatch.StartNew();

    //    try
    //    {
    //        process.Start();

    //        await process.StandardInput.WriteAsync(testCase.Input);
    //        process.StandardInput.Close();

    //        var stdoutTask = process.StandardOutput.ReadToEndAsync();
    //        var stderrTask = process.StandardError.ReadToEndAsync();
    //        var waitTask = process.WaitForExitAsync();
    //        long memoryUsedBytes = process.PeakWorkingSet64;

    //        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeoutMilliseconds)) == waitTask;

    //        testStopwatch.Stop();

    //        if (!completed)
    //        {
    //            try { process.Kill(true); } catch { }
    //            var timeoutResult = new TestCaseResult
    //            {
    //                TestCaseId = testCase.Id,
    //                Input = testCase.Input,
    //                ExpectedOutput = Normalize(testCase.ExpectedOutput),
    //                ActualOutput = "[TIME LIMIT EXCEEDED]",
    //                Passed = false,
    //                ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
    //                MemoryUsageBytes = memoryUsedBytes
    //            };
    //            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", timeoutResult);
    //            return timeoutResult;
    //        }

    //        string stdout = await stdoutTask;
    //        string stderr = await stderrTask;

    //        string actualOutput = Normalize(stdout);
    //        string expectedOutput = Normalize(testCase.ExpectedOutput);
    //        bool passed = actualOutput == expectedOutput;

    //        var testCaseResult = new TestCaseResult
    //        {
    //            TestCaseId = testCase.Id,
    //            Input = testCase.Input,
    //            ExpectedOutput = expectedOutput,
    //            ActualOutput = actualOutput,

    //            Passed = passed,
    //            ExecutionTimeMs = testStopwatch.ElapsedMilliseconds,
    //            MemoryUsageBytes = memoryUsedBytes
    //        };

    //        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", testCaseResult);
    //        return testCaseResult;
    //    }
    //    catch (Exception ex)
    //    {
    //        var errorResult = new TestCaseResult
    //        {
    //            TestCaseId = testCase.Id,
    //            Input = testCase.Input,
    //            ExpectedOutput = Normalize(testCase.ExpectedOutput),
    //            ActualOutput = $"[ERROR] {ex.Message}",
    //            Passed = false,
    //            ExecutionTimeMs = testStopwatch.ElapsedMilliseconds
    //        };

    //        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTestCaseResult", errorResult);
    //        return errorResult;
    //    }
    //}
}
