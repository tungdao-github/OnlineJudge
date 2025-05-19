namespace OnlineJudgeAPI.DTOs
{
    public class PasswordResetCode
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}