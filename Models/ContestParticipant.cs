using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeAPI.Models
{
    
    public class ContestParticipant
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int UserId { get; set; }
        public int TotalScore { get; set; } = 0;
        public DateTime LastSubmissionTime { get; set; }

        public Contest Contest { get; set; }
        public User User { get; set; }
    }
}
