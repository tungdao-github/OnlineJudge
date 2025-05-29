using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeAPI.Models
{
    
    public class ContestProblem
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int ProblemId { get; set; }
        public int Score { get; set; } = 100;
        public Contest Contest { get; set; }
        public Problem Problem { get; set; }
    }



}
