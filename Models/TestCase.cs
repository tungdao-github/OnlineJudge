using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OnlineJudgeAPI.Models
{

    public class TestCase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Input { get; set; }

        [Required]
        public string ExpectedOutput { get; set; }

        [ForeignKey("Problem")]
        public int ProblemId { get; set; }
        [JsonIgnore] // ✅ Prevents JSON validation issues
        public Problem? Problem { get; set; }


    }
}
