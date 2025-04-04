//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//public class AuthService
//{
//    private readonly IConfiguration _config;

//    public AuthService(IConfiguration config)
//    {
//        _config = config;
//    }

//    public string GenerateJwtToken(User user)
//    {
//        var claims = new List<Claim>
//        {
//            new Claim(ClaimTypes.Name, user.Username),
//            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
//        };

//        foreach (var role in user.Roles)
//        {
//            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
//        }

//        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
//        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//        var token = new JwtSecurityToken(
//            expires: DateTime.UtcNow.AddHours(1),
//            claims: claims,
//            signingCredentials: creds
//        );

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }
//}
