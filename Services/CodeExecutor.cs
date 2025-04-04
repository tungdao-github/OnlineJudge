using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeAPI.Services
{
    public class CodeExecutor
    {
        private const int TIMEOUT_MS = 10000; // 10 giây
        private const string CPP_COMPILER = "g++"; // Dùng g++ để biên dịch tối ưu

        public async Task<(string output, string error)> RunCodeAsync(string language, string code, string input)
        {
            string ext = language.ToLower() switch
            {
                "python" => ".py",
                "c++" => ".cpp",
                _ => throw new NotSupportedException("Ngôn ngữ không được hỗ trợ.")
            };

            string fileId = $"code_{Guid.NewGuid()}";
            string temp = Path.GetTempPath();
            string srcFile = Path.Combine(temp, fileId + ext);
            string exeFile = Path.Combine(temp, fileId + ".exe");

            try
            {
                await File.WriteAllTextAsync(srcFile, code, Encoding.UTF8);

                if (language.ToLower() == "c++")
                {
                    var compileError = await CompileCppAsync(srcFile, exeFile);
                    if (!string.IsNullOrWhiteSpace(compileError))
                        return ("", $"Lỗi biên dịch:\n{compileError}");
                }

                return await ExecuteAsync(language, srcFile, exeFile, input);
            }
            catch (Exception ex)
            {
                return ("", $"Lỗi hệ thống: {ex.Message}");
            }
            finally
            {
                _ = Task.Delay(1500).ContinueWith(_ => Cleanup(srcFile, exeFile));
            }
        }

        private async Task<string> CompileCppAsync(string srcPath, string outPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = CPP_COMPILER,
                Arguments = $"-std=c++17 -O3 -march=native -flto -o \"{outPath}\" \"{srcPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(outPath))
                return $"ExitCode: {process.ExitCode}\n{error}";

            return "";
        }

        private async Task<(string output, string error)> ExecuteAsync(string language, string srcFile, string exeFile, string input)
        {
            string fileName = language.ToLower() == "python" ? "python3" : exeFile;
            string args = language.ToLower() == "python" ? $"\"{srcFile}\"" : "";

            Console.WriteLine($"[DEBUG] FileName: {fileName}, Args: {args}");

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (!string.IsNullOrWhiteSpace(input))
            {
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var timeoutTask = Task.Delay(TIMEOUT_MS);
            var waitTask = process.WaitForExitAsync();

            if (await Task.WhenAny(timeoutTask, waitTask) == timeoutTask)
            {
                TryKill(process);
                return ("", "Thời gian thực thi vượt quá giới hạn.");
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(error))
                error = $"Chương trình kết thúc với mã lỗi: {process.ExitCode}";

            return (output.Trim(), error.Trim());
        }

        private void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(); // Dùng Kill() tránh platform issue
                    process.WaitForExit();
                }
            }
            catch { }
        }

        private void Cleanup(string source, string exe)
        {
            try { if (File.Exists(source)) File.Delete(source); } catch { }
            try { if (File.Exists(exe)) File.Delete(exe); } catch { }
        }
    }
}
