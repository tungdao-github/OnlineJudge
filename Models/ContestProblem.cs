using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeAPI.Models
{
    [Table("contestproblem")]
    public class ContestProblem
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int ProblemId { get; set; }

        public Contest Contest { get; set; }
        public Problem Problem { get; set; }
    }



}
