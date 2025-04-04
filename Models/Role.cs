using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OnlineJudgeAPI.Models;
public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RoleName { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
