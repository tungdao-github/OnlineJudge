using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Controllers;
public class CodeRunnerClient
{
    private readonly HttpClient _httpClient;

    public CodeRunnerClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("CodeRunnerClient");
    }

    public async Task<ExecutionResult> SubmitCodeAsync(SubmissionDto2 submission)
    {
        // var jsonContent = JsonSerializer.Serialize(submission);
        var jsonContent = JsonSerializer.Serialize(submission, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine("JSON gửi đi:\n" + jsonContent);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        Console.WriteLine(content);
        
        var response = await _httpClient.PostAsync("https://378b-2402-800-61d9-8256-985a-9409-351c-a314.ngrok-free.app/api/RunCode/submit", content);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error calling CodeRunner API: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExecutionResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result!;
    }
}
