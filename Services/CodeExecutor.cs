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
    public string Results = "";
    public  bool status = true;
    public int soDiemTrenTestCase = 0;
    public int Score = 0;
    private const int TimeoutMilliseconds = 9000;
    private readonly IHubContext<TestCaseResultHub> _hubContext;

    public CodeExecutor(IHubContext<TestCaseResultHub> hubContext)
    {
        _hubContext = hubContext;
    }

   
    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return string.Join("\n", s
            .Replace("\r\n", "\n")
            .Trim()
            .Split('\n')
            .Select(line => string.Join(" ", line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))) // trim nội dung giữa
            .Where(line => !string.IsNullOrWhiteSpace(line))
        );
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
            case "javascript":
            case "js":
                tempFile += ".js";
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
    public async Task<ExecutionResult> RunCodeAsync(string executablePath, List<TestCase> testCases, string language, string connectionId, int maxScore)
    {

        var result = new ExecutionResult();
        var totalStopwatch = Stopwatch.StartNew();
        Console.WriteLine("tesstcase.count = " + testCases.Count);

        var tasks = testCases.Select(testCase => 
            RunSingleTestCaseAsync(testCase, executablePath, language, connectionId)).ToList();

        var results = await Task.WhenAll(tasks);
        result.TestCaseResults.AddRange(results);
        // foreach(var vl in result.TestCaseResults) {
        //     Console.WriteLine(vl.Input + " " + vl.Passed);
        // }
        int passedCount = result.TestCaseResults.Count(r => r.Passed == true);
        int totalCount = testCases.Count;
        Console.WriteLine("tungdao = " +  passedCount +  " " +  totalCount);

        Score = (int)Math.Round((double)passedCount / totalCount * maxScore);
        Console.WriteLine("Score Code  = " + Score);
        result.TestCaseResults.AddRange(results);
        result.TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds;

        result.StandardOutput = string.Join("\n", results.Select(r => r.ActualOutput));
        result.StandardError = string.Join("\n", results.Where(r => !r.Passed).Select(r => r.ActualOutput.Contains("[ERROR]") ? r.ActualOutput : "").Where(x => !string.IsNullOrWhiteSpace(x)));

        return result;
    }

   
    private List<string> NormalizeLines(string s)
    {
        return s.Replace("\r\n", "\n").Trim().Split('\n').Select(line => line.Trim()).ToList();
    }

   

    
    public async Task<ExecutionResult> RunAndCompileCodeAsync(string code, List<TestCase> testCases, string language, string connectionId, int maxScore)
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

        var result = await RunCodeAsync(exePath, testCases, language, connectionId, maxScore);

        if (language != "python")
        {
            try { File.Delete(exePath); } catch { }
        }

        return result;
    }

    private async Task<TestCaseResult> RunSingleTestCaseAsync(TestCase testCase, string executablePath, string language, string connectionId)
{
        string fileName;
        string arguments;

        if (language == "python")
        {
            fileName = "python3";
            arguments = $"\"{executablePath}\"";
        }
        else if (language == "javascript" || language == "js")
        {
            fileName = "node";
            arguments = $"\"{executablePath}\"";
        }
        else
        {
            fileName = executablePath;
            arguments = "";
        }

        var psi = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
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
            var executionTime = DateTime.Now;
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
        // Console.WriteLine("" + actualOutput + " - " + expectedOutput);
        if(passed == false) {
                status = false;
        }
        else {
            status = true;
        }
        Results += "Testcase " + passed;
            double elapsedTime = (DateTime.Now - executionTime).TotalMilliseconds;
            var testCaseResult = new TestCaseResult
        {
            TestCaseId = testCase.Id,
            Input = testCase.Input,
            ExpectedOutput = expectedOutput,
            ActualOutput = actualOutput,
            Passed = passed,
            ExecutionTimeMs = elapsedTime,
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
    


}
