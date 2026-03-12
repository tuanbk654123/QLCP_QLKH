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
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IMongoCollection<AuditLog> _auditLogs;
    private readonly IPermissionService _permissionService;

    public AuditLogsController(IMongoClient client, IOptions<MongoDbSettings> options, IPermissionService permissionService)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _auditLogs = db.GetCollection<AuditLog>("audit_logs");
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string entityType, [FromQuery] int entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType)) return BadRequest(new { message = "Thiếu entityType" });
        if (entityId <= 0) return BadRequest(new { message = "Thiếu entityId" });

        if (!await CanViewAudit()) return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử" });

        var filter = Builders<AuditLog>.Filter.Eq(x => x.EntityType, entityType) &
                     Builders<AuditLog>.Filter.Eq(x => x.EntityLegacyId, entityId);

        var items = await _auditLogs.Find(filter).SortByDescending(x => x.CreatedAt).Limit(200).ToListAsync();

        return Ok(new
        {
            items = items.Select(x => new
            {
                id = x.Id,
                action = x.Action,
                createdAt = x.CreatedAt,
                actorUserId = x.ActorUserId,
                actorFullName = x.ActorFullName,
                actorPosition = x.ActorPosition,
                actorRole = x.ActorRole,
                changedFields = x.ChangedFields
            })
        });
    }

    [HttpGet("recent")]
    public async Task<ActionResult<object>> Recent(
        [FromQuery] string? module,
        [FromQuery] string? entityType,
        [FromQuery] int? entityId,
        [FromQuery] string? action,
        [FromQuery] int? actorUserId,
        [FromQuery] string? actorName,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var viewable = await GetViewableEntityTypes();
        if (viewable.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử" });

        if (!string.IsNullOrWhiteSpace(module))
        {
            var moduleValue = module.Trim().ToLowerInvariant();
            if (moduleValue == "qlkh")
            {
                viewable = new HashSet<string>(new[] { "customer" }, StringComparer.OrdinalIgnoreCase);
            }
            else if (moduleValue == "qlcp")
            {
                viewable = new HashSet<string>(new[] { "cost" }, StringComparer.OrdinalIgnoreCase);
            }
        }

        if (viewable.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử" });

        var filter = Builders<AuditLog>.Filter.In(x => x.EntityType, viewable);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var t = entityType.Trim().ToLowerInvariant();
            if (!viewable.Contains(t)) return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử" });
            filter &= Builders<AuditLog>.Filter.Eq(x => x.EntityType, t);
        }

        if (entityId.HasValue && entityId.Value > 0)
        {
            filter &= Builders<AuditLog>.Filter.Eq(x => x.EntityLegacyId, entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            filter &= Builders<AuditLog>.Filter.Eq(x => x.Action, action.Trim().ToLowerInvariant());
        }

        if (actorUserId.HasValue && actorUserId.Value > 0)
        {
            filter &= Builders<AuditLog>.Filter.Eq(x => x.ActorUserId, actorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(actorName))
        {
            var pattern = actorName.Trim();
            filter &= Builders<AuditLog>.Filter.Regex(x => x.ActorFullName, new BsonRegularExpression(pattern, "i"));
        }

        if (!string.IsNullOrWhiteSpace(from))
        {
            filter &= Builders<AuditLog>.Filter.Gte(x => x.CreatedAt, from.Trim());
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            filter &= Builders<AuditLog>.Filter.Lte(x => x.CreatedAt, to.Trim());
        }

        var total = await _auditLogs.CountDocumentsAsync(filter);
        var items = await _auditLogs
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = items.Select(x => new
            {
                id = x.Id,
                entityType = x.EntityType,
                entityId = x.EntityLegacyId,
                action = x.Action,
                createdAt = x.CreatedAt,
                actorUserId = x.ActorUserId,
                actorFullName = x.ActorFullName,
                actorPosition = x.ActorPosition,
                actorRole = x.ActorRole,
                changedFields = x.ChangedFields
            })
        });
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<object>> Detail(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "Thiếu id" });

        AuditLog? log;
        try
        {
            log = await _auditLogs.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Id không hợp lệ" });
        }

        if (log == null) return NotFound(new { message = "Không tìm thấy lịch sử" });

        if (!await CanViewAudit()) return StatusCode(403, new { message = "Bạn không có quyền xem lịch sử" });

        return Ok(new
        {
            id = log.Id,
            entityType = log.EntityType,
            entityId = log.EntityLegacyId,
            action = log.Action,
            createdAt = log.CreatedAt,
            actorUserId = log.ActorUserId,
            actorFullName = log.ActorFullName,
            actorPosition = log.ActorPosition,
            actorRole = log.ActorRole,
            changedFields = log.ChangedFields,
            oldData = ToPlain(log.OldData),
            newData = ToPlain(log.NewData)
        });
    }

    private async Task<HashSet<string>> GetViewableEntityTypes()
    {
        if (!await CanViewAudit()) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new HashSet<string>(new[] { "customer", "cost" }, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> CanViewAudit()
    {
        var roleCode = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(roleCode)) return false;
        if (string.Equals(roleCode, "admin", StringComparison.OrdinalIgnoreCase)) return true;

        var permissions = await _permissionService.GetRolePermissionsForModuleAsync("audit", roleCode);
        if (!permissions.TryGetValue("view", out var level)) return false;
        return !string.Equals(level, "N", StringComparison.OrdinalIgnoreCase);
    }

    private static object? ToPlain(BsonDocument? doc)
    {
        if (doc == null) return null;
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var el in doc.Elements)
        {
            dict[el.Name] = ToPlainValue(el.Value);
        }
        return dict;
    }

    private static object? ToPlainValue(BsonValue val)
    {
        return val.BsonType switch
        {
            BsonType.Null => null,
            BsonType.Boolean => val.AsBoolean,
            BsonType.Int32 => val.AsInt32,
            BsonType.Int64 => val.AsInt64,
            BsonType.Double => val.AsDouble,
            BsonType.Decimal128 => Decimal128.ToDecimal(val.AsDecimal128),
            BsonType.String => val.AsString,
            BsonType.ObjectId => val.AsObjectId.ToString(),
            BsonType.DateTime => val.ToUniversalTime(),
            BsonType.Document => ToPlain(val.AsBsonDocument),
            BsonType.Array => val.AsBsonArray.Select(ToPlainValue).ToList(),
            _ => val.ToString()
        };
    }
}
