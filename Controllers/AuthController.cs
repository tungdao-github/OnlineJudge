using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using OnlineJudgeAPI.DTOs;
using OnlineJudgeAPI.Models;
using OnlineJudgeAPI.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // [HttpPost("register")]
    // public async Task<IActionResult> Register(RegisterDto dto)
    // {
    //     Console.WriteLine(dto.Username + " " + dto.Password + " " + dto.Email + " " + dto.RoleIds);
    //     if (await _context.Users.AnyAsync(u=> u.Username == dto.Username || u.Email == dto.Email))
    //     {
    //         return BadRequest("Username or email already exists.");
    //     }

    //     var roles = await _context.Roles.Where(r => dto.RoleIds.Contains(r.Id)).ToListAsync();

    //     var user = new User
    //     {
    //         Username = dto.Username,
    //         Email = dto.Email,
    //         PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
    //         UserRoles = roles.Select(r => new UserRole { RoleId = r.Id }).ToList()
    //     };

    //     _context.Users.Add(user);
    //     await _context.SaveChangesAsync();

    //     return Ok(new { message = "Registered successfully" });
    // }
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto) {
        var user = await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username==dto.Username);
        if(await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username)) {
            return BadRequest("Tai khoan hoac mat khau da ton tai");
        }
        else{
            await _context.Users.AddAsync(new User{Email = dto.Email, Username = dto.Username, PasswordHash  = dto.Password, UserRoles = _context.Roles.Select(r => new UserRole { RoleId = r.Id }).ToList() });
            await _context.SaveChangesAsync();
            return Ok(new{message = "Dang ki thanh cong"});
        }
    }

    // [HttpPost("login")]
    // public async Task<IActionResult> Login(LoginDto dto)
    // {
    //     // var user = await _context.Users.Include(u => u.UserRoles)
    //     //     .ThenInclude(ur => ur.Role)
    //     //     .FirstOrDefaultAsync(u => u.Username == dto.Username);
    //     var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

    //     if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
    //         return Unauthorized("Invalid credentials");

    //     var token = GenerateJwtToken(user);

    //     return Ok(new { token });
    // }
    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginDto dto) {
        var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
        if(user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) {
            return Unauthorized("Tai khoan hoac mat khau khong dung");
        }
        else {
            var token = GenerateJwtToken(user);
            return Ok(new {token, userId =user.Id});
        }
        
    }
    //private string GenerateJwtToken(User user)
    //{
    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]));
    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var claims = new List<Claim>
    //    {
    //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    //        new Claim(ClaimTypes.Name, user.Username)
    //    };

    //    claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.RoleName)));
    //        Console.WriteLine(string.Join(",", claims.Select(c => $"{c.Type}:{c.Value}")));

    //    var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
    //    Console.WriteLine("[DEBUG] Role: " + role);
    //    var token = new JwtSecurityToken(
    //        issuer: _config["JwtSettings:Issuer"],
    //        audience: _config["JwtSettings:Audience"],
    //        claims: claims,
    //        expires: DateTime.Now.AddMinutes(60),
    //        signingCredentials: creds
    //    );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim("userId", user.Id.ToString()), // ✅ Thêm claim userId để backend đọc được
        new Claim(ClaimTypes.Name, user.Username)
    };

        // Thêm role vào claim
        claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.RoleName)));

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
