namespace OnlineJudgeAPI.Services
{
    public class JudgeService
    {
        public async Task<bool> EvaluateAsync(string code, string input, string expectedOutput)
        {
            // Giả lập chấm điểm - bạn có thể thay bằng CodeExecutor thật
            await Task.Delay(100); // Giả delay
            return expectedOutput.Trim() == input.Trim(); // Giả sử code là echo
        }
    }
}
