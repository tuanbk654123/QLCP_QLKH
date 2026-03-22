using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMongoCollection<Role> _roles;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<FieldPermission> _fieldPermissions;

    public RolesController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _roles = db.GetCollection<Role>("roles");
        _users = db.GetCollection<User>("users");
        _fieldPermissions = db.GetCollection<FieldPermission>("field_permissions");
    }

    public class UpsertRoleRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    private static string NormalizeCode(string code)
    {
        return (code ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool IsValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        foreach (var ch in code)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-';
            if (!ok) return false;
        }
        return true;
    }

    [HttpGet]
    public async Task<ActionResult<object>> ListActive()
    {
        var roles = await _roles.Find(r => r.IsActive).SortBy(r => r.Name).ToListAsync();
        return Ok(new
        {
            items = roles.Select(r => new
            {
                id = r.Id,
                code = r.Code,
                name = r.Name,
                description = r.Description,
                isActive = r.IsActive,
                isSystem = r.IsSystem
            })
        });
    }

    [HttpGet("all")]
    [Authorize(Roles = "admin,ceo,assistant_ceo")]
    public async Task<ActionResult<object>> ListAll()
    {
        var roles = await _roles.Find(_ => true).SortByDescending(r => r.IsSystem).ThenBy(r => r.Name).ToListAsync();
        return Ok(new
        {
            items = roles.Select(r => new
            {
                id = r.Id,
                code = r.Code,
                name = r.Name,
                description = r.Description,
                isActive = r.IsActive,
                isSystem = r.IsSystem
            })
        });
    }

    [HttpPost]
    [Authorize(Roles = "admin,ceo,assistant_ceo")]
    public async Task<ActionResult<object>> Create([FromBody] UpsertRoleRequest input)
    {
        var code = NormalizeCode(input.Code);
        if (!IsValidCode(code)) return BadRequest(new { message = "Role code không hợp lệ (chỉ a-z, 0-9, _, -)" });
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { message = "Thiếu tên chức danh" });

        var exists = await _roles.Find(r => r.Code == code).AnyAsync();
        if (exists) return BadRequest(new { message = "Role code đã tồn tại" });

        var entity = new Role
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Code = code,
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            IsActive = input.IsActive,
            IsSystem = false
        };

        await _roles.InsertOneAsync(entity);

        return Ok(new
        {
            id = entity.Id,
            code = entity.Code,
            name = entity.Name,
            description = entity.Description,
            isActive = entity.IsActive,
            isSystem = entity.IsSystem
        });
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "admin,ceo,assistant_ceo")]
    public async Task<ActionResult<object>> Update(string code, [FromBody] UpsertRoleRequest input)
    {
        var normalized = NormalizeCode(code);
        var role = await _roles.Find(r => r.Code == normalized).FirstOrDefaultAsync();
        if (role == null) return NotFound(new { message = "Không tìm thấy chức danh" });

        if (role.IsSystem)
        {
            if (!string.IsNullOrWhiteSpace(input.Code) && NormalizeCode(input.Code) != role.Code)
            {
                return BadRequest(new { message = "Không thể đổi code của chức danh hệ thống" });
            }

            if (input.IsActive == false)
            {
                return BadRequest(new { message = "Không thể tắt chức danh hệ thống" });
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Code))
        {
            var newCode = NormalizeCode(input.Code);
            if (!IsValidCode(newCode)) return BadRequest(new { message = "Role code không hợp lệ" });
            if (!string.Equals(newCode, role.Code, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _roles.Find(r => r.Code == newCode).AnyAsync();
                if (exists) return BadRequest(new { message = "Role code đã tồn tại" });
                role.Code = newCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(input.Name)) role.Name = input.Name.Trim();
        role.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        role.IsActive = input.IsActive;

        await _roles.ReplaceOneAsync(r => r.Id == role.Id, role);

        return Ok(new
        {
            id = role.Id,
            code = role.Code,
            name = role.Name,
            description = role.Description,
            isActive = role.IsActive,
            isSystem = role.IsSystem
        });
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "admin,ceo,assistant_ceo")]
    public async Task<ActionResult<object>> Delete(string code)
    {
        var normalized = NormalizeCode(code);
        var role = await _roles.Find(r => r.Code == normalized).FirstOrDefaultAsync();
        if (role == null) return NotFound(new { message = "Không tìm thấy chức danh" });
        if (role.IsSystem) return BadRequest(new { message = "Không thể xóa chức danh hệ thống" });

        var usedByUsers = await _users.Find(u => u.RoleCode == normalized).Limit(1).AnyAsync();
        if (usedByUsers)
        {
            role.IsActive = false;
            await _roles.ReplaceOneAsync(r => r.Id == role.Id, role);
            return Ok(new { message = "Đã ngừng kích hoạt chức danh (đang được gán cho nhân viên)" });
        }

        var usedInPermissions = await _fieldPermissions.Find(p => p.RoleCode == normalized).Limit(1).AnyAsync();
        if (usedInPermissions)
        {
            role.IsActive = false;
            await _roles.ReplaceOneAsync(r => r.Id == role.Id, role);
            return Ok(new { message = "Đã ngừng kích hoạt chức danh (đang có dữ liệu phân quyền)" });
        }

        await _roles.DeleteOneAsync(r => r.Id == role.Id);
        return Ok(new { message = "Đã xóa chức danh" });
    }
}

