using System.ComponentModel.DataAnnotations;
using OnlineJudgeAPI.Models;
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; }

    // Quan hệ nhiều-nhiều
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}