using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly AuthSettings _authSettings;

    public AuthController(IMongoClient client, IOptions<MongoDbSettings> mongoOptions, IOptions<AuthSettings> authOptions)
    {
        var db = client.GetDatabase(mongoOptions.Value.DatabaseName);
        _users = db.GetCollection<User>("users");
        _authSettings = authOptions.Value;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
        
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
    {
        Console.WriteLine($"Login attempt for user: {request.Username}");
        var user = await _users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        if (user == null)
        {
            Console.WriteLine($"User not found: {request.Username}");
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            Console.WriteLine($"Invalid password for user: {request.Username}");
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
        }

        if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"User inactive: {request.Username}");
            return Unauthorized(new { message = "Tài khoản đang không hoạt động hoặc đã nghỉ việc" });
        }

        Console.WriteLine($"Login successful for user: {request.Username}");
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.LegacyId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone,
                address = user.Address,
                role = user.RoleCode,
                status = user.Status,
                department = user.Department,
                position = user.Position,
                joinDate = user.JoinDate
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> Me()
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        if (legacyIdClaim == null || !int.TryParse(legacyIdClaim, out var legacyId))
        {
            return Unauthorized();
        }

        var user = await _users.Find(u => u.LegacyId == legacyId).FirstOrDefaultAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(user.Status) &&
            !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = "Tài khoản đang không hoạt động hoặc đã nghỉ việc" });
        }

        return Ok(new
        {
            user = new
            {
                id = user.LegacyId,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phone = user.Phone,
                address = user.Address,
                role = user.RoleCode,
                status = user.Status,
                department = user.Department,
                position = user.Position,
                joinDate = user.JoinDate
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult<object> Logout()
    {
        return Ok(new { message = "Đăng xuất thành công" });
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        // 1. Check if it matches the hash
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(passwordBytes);
        var hashString = Convert.ToHexString(hashBytes);
        if (string.Equals(hashString, storedHash, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 2. Check if it matches plain text (Legacy support)
        if (storedHash == password)
        {
            return true;
        }

        return false;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("legacy_id", user.LegacyId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.RoleCode)
        };

        var token = new JwtSecurityToken(
            issuer: _authSettings.Issuer,
            audience: _authSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_authSettings.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
