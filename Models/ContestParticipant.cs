using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeAPI.Models
{
    [Table("contestparticipant")]
    public class ContestParticipant
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int UserId { get; set; }

        public Contest Contest { get; set; }
        public User User { get; set; }
    }
}
