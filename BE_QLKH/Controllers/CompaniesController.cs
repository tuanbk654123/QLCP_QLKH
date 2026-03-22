using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IMongoCollection<Company> _companies;
    private readonly IPermissionService _permissionService;

    public CompaniesController(IMongoClient client, IOptions<MongoDbSettings> options, IPermissionService permissionService)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _companies = db.GetCollection<Company>("companies");
        _permissionService = permissionService;
    }

    private static string? GetRoleCode(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value;

    private static bool Allows(string? level, string required)
    {
        level = string.IsNullOrWhiteSpace(level) ? "N" : level.Trim().ToUpperInvariant();
        required = string.IsNullOrWhiteSpace(required) ? "R" : required.Trim().ToUpperInvariant();

        if (level == "A") return true;
        if (required == "A") return false;
        if (level == "W") return required == "W" || required == "R";
        if (level == "R") return required == "R";
        return false;
    }

    private async Task<bool> HasCompanyPermission(string field, string required)
    {
        var roleCode = GetRoleCode(User);
        if (string.IsNullOrWhiteSpace(roleCode)) return false;
        var perms = await _permissionService.GetRolePermissionsForModuleAsync("companies", roleCode);
        perms.TryGetValue(field, out var level);
        return Allows(level, required);
    }

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? search)
    {
        if (!await HasCompanyPermission("view", "R")) return StatusCode(403, new { message = "Bạn không có quyền" });

        var filter = Builders<Company>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var rx = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search.Trim()), "i");
            filter &= Builders<Company>.Filter.Or(
                Builders<Company>.Filter.Regex(c => c.Code, rx),
                Builders<Company>.Filter.Regex(c => c.Name, rx)
            );
        }

        var items = await _companies.Find(filter).SortBy(c => c.Code).ToListAsync();
        return Ok(new
        {
            items = items.Select(c => new
            {
                id = c.Id,
                code = c.Code,
                name = c.Name,
                domain = c.Domain,
                status = c.Status,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt
            })
        });
    }

    public class UpsertCompanyRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string Status { get; set; } = "active";
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertCompanyRequest input)
    {
        if (!await HasCompanyPermission("manage", "W")) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (string.IsNullOrWhiteSpace(input.Code)) return BadRequest(new { message = "Thiếu code" });
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { message = "Thiếu name" });

        var code = input.Code.Trim().ToUpperInvariant();
        var exists = await _companies.Find(c => c.Code == code).AnyAsync();
        if (exists) return BadRequest(new { message = "Code đã tồn tại" });

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var actorId = GetActorLegacyId();

        var entity = new Company
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Code = code,
            Name = input.Name.Trim(),
            Domain = string.IsNullOrWhiteSpace(input.Domain) ? null : input.Domain.Trim(),
            Status = string.IsNullOrWhiteSpace(input.Status) ? "active" : input.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorId,
            UpdatedBy = actorId
        };

        await _companies.InsertOneAsync(entity);

        return Ok(new
        {
            id = entity.Id,
            code = entity.Code,
            name = entity.Name,
            domain = entity.Domain,
            status = entity.Status
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<object>> Update(string id, [FromBody] UpsertCompanyRequest input)
    {
        if (!await HasCompanyPermission("manage", "W")) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (!ObjectId.TryParse(id, out _)) return BadRequest(new { message = "Id không hợp lệ" });

        var company = await _companies.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (company == null) return NotFound(new { message = "Không tìm thấy công ty" });

        if (!string.IsNullOrWhiteSpace(input.Code))
        {
            var code = input.Code.Trim().ToUpperInvariant();
            var exists = await _companies.Find(c => c.Code == code && c.Id != id).AnyAsync();
            if (exists) return BadRequest(new { message = "Code đã tồn tại" });
            company.Code = code;
        }

        if (!string.IsNullOrWhiteSpace(input.Name)) company.Name = input.Name.Trim();
        company.Domain = string.IsNullOrWhiteSpace(input.Domain) ? null : input.Domain.Trim();
        if (!string.IsNullOrWhiteSpace(input.Status)) company.Status = input.Status.Trim();

        company.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        company.UpdatedBy = GetActorLegacyId();

        await _companies.ReplaceOneAsync(c => c.Id == id, company);
        return Ok(new { message = "OK" });
    }

    private int GetActorLegacyId()
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdClaim, out var legacyId) ? legacyId : 0;
    }
}
