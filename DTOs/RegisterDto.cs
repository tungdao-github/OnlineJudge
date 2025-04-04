namespace OnlineJudgeAPI.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>(); // Truyền danh sách ID của Role
    }
}
