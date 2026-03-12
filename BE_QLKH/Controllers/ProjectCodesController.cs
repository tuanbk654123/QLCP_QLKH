using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/project-codes")]
[Authorize]
public class ProjectCodesController : ControllerBase
{
    private readonly IMongoCollection<ProjectCode> _projectCodes;

    public ProjectCodesController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _projectCodes = db.GetCollection<ProjectCode>("project_codes");
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<ActionResult<object>> List()
    {
        var items = await _projectCodes.Find(_ => true).SortBy(x => x.LegacyId).ToListAsync();
        var result = items.Select(x => new { id = x.LegacyId, code = x.Code }).ToList();
        return Ok(new { items = result });
    }

    public class UpsertRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertRequest req)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new { message = "Mã dự án không được để trống" });

        var normalized = req.Code.Trim();
        var existed = await _projectCodes.Find(x => x.Code == normalized).FirstOrDefaultAsync();
        if (existed != null) return Ok(new { id = existed.LegacyId, code = existed.Code });

        var now = DateTime.Now.ToString("yyyy-MM-dd");
        var maxLegacyId = await _projectCodes.Find(_ => true).SortByDescending(x => x.LegacyId).Limit(1).FirstOrDefaultAsync();
        var nextLegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        var item = new ProjectCode
        {
            Id = ObjectId.GenerateNewId().ToString(),
            LegacyId = nextLegacyId,
            Code = normalized,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _projectCodes.InsertOneAsync(item);
        return Ok(new { id = item.LegacyId, code = item.Code });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] UpsertRequest req)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new { message = "Mã dự án không được để trống" });

        var item = await _projectCodes.Find(x => x.LegacyId == id).FirstOrDefaultAsync();
        if (item == null) return NotFound(new { message = "Không tìm thấy mã dự án" });

        item.Code = req.Code.Trim();
        item.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd");

        await _projectCodes.ReplaceOneAsync(x => x.Id == item.Id, item);
        return Ok(new { id = item.LegacyId, code = item.Code });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> Delete(int id)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });

        var result = await _projectCodes.DeleteOneAsync(x => x.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "Không tìm thấy mã dự án" });
        return Ok(new { message = "Đã xóa" });
    }
}

