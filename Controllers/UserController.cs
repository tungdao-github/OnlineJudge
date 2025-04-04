using Microsoft.AspNetCore.Mvc;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.Services;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.DTOs;
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/User/Register
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == user.Username);
        if (existingUser != null)
            return BadRequest("Username already taken.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // Hash password
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully!" });
    }

    // POST: api/User/Login
    //[HttpPost("Login")]
    //public async Task<IActionResult> Login([FromBody] User loginData)
    //{
    //    var user = await _context.Users
    //        .FirstOrDefaultAsync(u => u.Username == loginData.Username);
    //    if (user == null || !BCrypt.Net.BCrypt.Verify(loginData.PasswordHash, user.PasswordHash))
    //        return Unauthorized("Invalid username or password.");

    //    return Ok(new { message = "Login successful!" });
    //}
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginData)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == loginData.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginData.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password.");

        return Ok(new { message = "Login successful!" });
    }
    //public class LoginDto
    //{
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //}
}
