using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public List<TestCase>? TestCases { get; set; } = new List<TestCase>() { new TestCase() { } };
    }

}
