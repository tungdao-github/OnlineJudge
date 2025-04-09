using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineJudgeAPI.Models
{
    
    public class Problem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string? InputFormat { get; set; } = "";
        public string? OutputFormat { get; set; } = "";
        public string? InputSample { get; set; } = "";
        public string? OutputSample { get; set; } = "";
        public List<TestCase>? TestCases { get; set; } = new List<TestCase>() { new TestCase() { } };
        [JsonIgnore]
    public ICollection<Submission>? Submissions { get; set; }

    }

}
