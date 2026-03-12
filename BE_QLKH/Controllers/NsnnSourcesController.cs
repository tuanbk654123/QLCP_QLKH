using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/nsnn-sources")]
[Authorize]
public class NsnnSourcesController : ControllerBase
{
    private readonly IMongoCollection<NsnnSource> _nsnnSources;

    public NsnnSourcesController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _nsnnSources = db.GetCollection<NsnnSource>("nsnn_sources");
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<ActionResult<object>> List()
    {
        var items = await _nsnnSources.Find(_ => true).SortBy(x => x.LegacyId).ToListAsync();
        var result = items.Select(x => new { id = x.LegacyId, name = x.Name }).ToList();
        return Ok(new { items = result });
    }

    public class UpsertRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UpsertRequest req)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Tên nguồn không được để trống" });

        var normalized = req.Name.Trim();
        var existed = await _nsnnSources.Find(x => x.Name == normalized).FirstOrDefaultAsync();
        if (existed != null) return Ok(new { id = existed.LegacyId, name = existed.Name });

        var now = DateTime.Now.ToString("yyyy-MM-dd");
        var maxLegacyId = await _nsnnSources.Find(_ => true).SortByDescending(x => x.LegacyId).Limit(1).FirstOrDefaultAsync();
        var nextLegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        var item = new NsnnSource
        {
            Id = ObjectId.GenerateNewId().ToString(),
            LegacyId = nextLegacyId,
            Name = normalized,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _nsnnSources.InsertOneAsync(item);
        return Ok(new { id = item.LegacyId, name = item.Name });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] UpsertRequest req)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Tên nguồn không được để trống" });

        var item = await _nsnnSources.Find(x => x.LegacyId == id).FirstOrDefaultAsync();
        if (item == null) return NotFound(new { message = "Không tìm thấy nguồn" });

        var normalized = req.Name.Trim();
        item.Name = normalized;
        item.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd");

        await _nsnnSources.ReplaceOneAsync(x => x.Id == item.Id, item);
        return Ok(new { id = item.LegacyId, name = item.Name });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> Delete(int id)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Bạn không có quyền" });

        var result = await _nsnnSources.DeleteOneAsync(x => x.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "Không tìm thấy nguồn" });
        return Ok(new { message = "Đã xóa" });
    }
}

