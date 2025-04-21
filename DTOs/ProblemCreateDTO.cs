using OnlineJudgeAPI.Models;

namespace OnlineJudgeAPI.DTOs
{
    public class ProblemCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string? InputFormat { get; set; }
        public string? OutputFormat { get; set; }
        public string? Constraints{get; set;}
        public string? DoKho{get; set;}
        public string DangBai {get; set;}
        public string? InputSample { get; set; }
        public string? OutputSample { get; set; }
        public int ContestId { get; set; }
        //public string ExpectedOutput { get; set; }
        public List<TestCase> TestCases { get; set; }
    }

}
