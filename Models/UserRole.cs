using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace OnlineJudgeAPI.Models
{
    
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public int RoleId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User User { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }
    }

}
