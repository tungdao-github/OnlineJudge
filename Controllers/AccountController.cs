using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Helpers;
using OnlineJudgeAPI.Services;
using ForgotPasswordRequest = Microsoft.AspNetCore.Identity.Data.ForgotPasswordRequest;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly EmailService _emailService;

    public AccountController(ApplicationDbContext dbContext, IConfiguration config, EmailService emailService)
    {
        _dbContext = dbContext;
        _config = config;
        _emailService = emailService;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return NotFound("Email không tồn tại");

        // Tạo mã code 6 chữ số
        var code = new Random().Next(100000, 999999).ToString();

        // Lưu mã vào DB (xóa mã cũ nếu có)
        var oldCode = await _dbContext.PasswordResetCodes.FirstOrDefaultAsync(c => c.Email == request.Email);
        if (oldCode != null) _dbContext.PasswordResetCodes.Remove(oldCode);

        var resetCode = new PasswordResetCode
        {
            Email = request.Email,
            Code = code,
            ExpireAt = DateTime.UtcNow.AddMinutes(1)
        };
        await _dbContext.PasswordResetCodes.AddAsync(resetCode);
        await _dbContext.SaveChangesAsync();

        await _emailService.SendAsync(user.Email, "Mã xác nhận đặt lại mật khẩu",
            $"Mã xác nhận của bạn là: <b>{code}</b> (có hiệu lực trong 15 phút)");

        return Ok("Đã gửi mã xác nhận đến email");
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        var codeEntry = await _dbContext.PasswordResetCodes
            .FirstOrDefaultAsync(c => c.Email == request.Email && c.Code == request.Code);

        if (codeEntry == null || codeEntry.ExpireAt < DateTime.UtcNow)
            return BadRequest("Mã không đúng hoặc đã hết hạn");

        // Nếu hợp lệ, tạo một JWT token cho bước reset mật khẩu
        var token = JwtTokenHelper.GenerateResetPasswordToken(request.Email, _config["Jwt:Key"], 15);

        return Ok(new { token });
    }

    public class VerifyCodeRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest("Mật khẩu xác nhận không khớp");

        string email;
        try
        {
            email = JwtTokenHelper.ValidateResetToken(request.Token, _config["Jwt:Key"]);
        }
        catch
        {
            return BadRequest("Token không hợp lệ hoặc đã hết hạn");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("Người dùng không tồn tại");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _dbContext.SaveChangesAsync();

        return Ok("Đặt lại mật khẩu thành công");
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}