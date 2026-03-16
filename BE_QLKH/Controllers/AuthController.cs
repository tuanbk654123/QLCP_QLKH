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
    private readonly IMongoCollection<UserCompany> _userCompanies;
    private readonly IMongoCollection<Company> _companies;
    private readonly AuthSettings _authSettings;

    public AuthController(IMongoClient client, IOptions<MongoDbSettings> mongoOptions, IOptions<AuthSettings> authOptions)
    {
        var db = client.GetDatabase(mongoOptions.Value.DatabaseName);
        _users = db.GetCollection<User>("users");
        _userCompanies = db.GetCollection<UserCompany>("user_companies");
        _companies = db.GetCollection<Company>("companies");
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
        var activeCompanyId = await ResolveActiveCompanyId(user);
        var token = GenerateJwtToken(user, activeCompanyId);

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
                joinDate = user.JoinDate,
                companyId = activeCompanyId
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

        var activeCompanyId = BE_QLKH.Services.TenantContext.GetCompanyId(User) ?? await ResolveActiveCompanyId(user);

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
                joinDate = user.JoinDate,
                companyId = activeCompanyId
            }
        });
    }

    [HttpGet("companies")]
    [Authorize]
    public async Task<ActionResult<object>> Companies()
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        if (legacyIdClaim == null || !int.TryParse(legacyIdClaim, out var legacyId))
        {
            return Unauthorized();
        }

        var mappings = await _userCompanies.Find(x => x.UserLegacyId == legacyId).ToListAsync();
        var companyIds = mappings.Select(x => x.CompanyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var companies = companyIds.Count == 0
            ? new List<Company>()
            : await _companies.Find(c => companyIds.Contains(c.Id) && c.Status == "active").ToListAsync();

        var activeCompanyId = BE_QLKH.Services.TenantContext.GetCompanyId(User);

        return Ok(new
        {
            activeCompanyId,
            items = companies.Select(c => new { id = c.Id, code = c.Code, name = c.Name }).OrderBy(x => x.code).ToList()
        });
    }

    public class SwitchCompanyRequest
    {
        public string CompanyId { get; set; } = string.Empty;
    }

    [HttpPost("switch-company")]
    [Authorize]
    public async Task<ActionResult<object>> SwitchCompany([FromBody] SwitchCompanyRequest request)
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        if (legacyIdClaim == null || !int.TryParse(legacyIdClaim, out var legacyId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CompanyId))
        {
            return BadRequest(new { message = "Thiếu companyId" });
        }

        var ok = await _userCompanies.Find(x => x.UserLegacyId == legacyId && x.CompanyId == request.CompanyId).AnyAsync();
        if (!ok) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });

        var user = await _users.Find(u => u.LegacyId == legacyId).FirstOrDefaultAsync();
        if (user == null) return Unauthorized();

        var token = GenerateJwtToken(user, request.CompanyId);
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
                joinDate = user.JoinDate,
                companyId = request.CompanyId
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

    private string GenerateJwtToken(User user, string companyId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("legacy_id", user.LegacyId.ToString()),
            new Claim("company_id", companyId),
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

    private async Task<string> ResolveActiveCompanyId(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.CompanyId))
        {
            var ok = await _userCompanies.Find(x => x.UserLegacyId == user.LegacyId && x.CompanyId == user.CompanyId).AnyAsync();
            if (ok) return user.CompanyId;
        }

        var mapping = await _userCompanies
            .Find(x => x.UserLegacyId == user.LegacyId)
            .SortByDescending(x => x.IsDefault)
            .FirstOrDefaultAsync();

        if (mapping != null && !string.IsNullOrWhiteSpace(mapping.CompanyId)) return mapping.CompanyId;
        return user.CompanyId;
    }
}
