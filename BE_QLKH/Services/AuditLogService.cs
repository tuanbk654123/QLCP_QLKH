using System.Text.Json;
using BE_QLKH.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BE_QLKH.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IMongoCollection<AuditLog> _auditLogs;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditLogService(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _auditLogs = db.GetCollection<AuditLog>("audit_logs");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task LogAsync(string entityType, int entityLegacyId, string action, User? actor, object? oldData, object? newData)
    {
        var oldDoc = ToBson(oldData);
        var newDoc = ToBson(newData);
        var changedFields = ComputeChangedFields(entityType, oldDoc, newDoc, action);

        var log = new AuditLog
        {
            Id = ObjectId.GenerateNewId().ToString(),
            EntityType = entityType,
            EntityLegacyId = entityLegacyId,
            CompanyId = GetCompanyId(actor, oldDoc, newDoc),
            Action = action,
            ActorUserId = actor?.LegacyId ?? 0,
            ActorFullName = actor?.FullName ?? string.Empty,
            ActorPosition = actor?.Position ?? string.Empty,
            ActorRole = actor?.RoleCode ?? string.Empty,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            OldData = oldDoc,
            NewData = newDoc,
            ChangedFields = changedFields
        };

        await _auditLogs.InsertOneAsync(log);
    }

    private static string GetCompanyId(User? actor, BsonDocument? oldDoc, BsonDocument? newDoc)
    {
        if (actor != null && !string.IsNullOrWhiteSpace(actor.CompanyId)) return actor.CompanyId;

        if (newDoc != null)
        {
            if (newDoc.TryGetValue("companyId", out var v1) && v1.IsString) return v1.AsString;
            if (newDoc.TryGetValue("company_id", out var v2) && v2.IsString) return v2.AsString;
        }

        if (oldDoc != null)
        {
            if (oldDoc.TryGetValue("companyId", out var v1) && v1.IsString) return v1.AsString;
            if (oldDoc.TryGetValue("company_id", out var v2) && v2.IsString) return v2.AsString;
        }

        return string.Empty;
    }

    private BsonDocument? ToBson(object? data)
    {
        if (data == null) return null;
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            return BsonDocument.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ComputeChangedFields(string entityType, BsonDocument? oldDoc, BsonDocument? newDoc, string action)
    {
        var actionValue = action?.Trim() ?? string.Empty;
        var isDiffAction =
            string.Equals(actionValue, "update", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionValue, "approve", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actionValue, "reject", StringComparison.OrdinalIgnoreCase);

        if (!isDiffAction) return new List<string>();
        if (oldDoc == null || newDoc == null) return new List<string>();

        var ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "updatedAt",
            "updatedBy",
            "updatedByName",
            "createdAt",
            "createdBy",
            "createdByName"
        };

        if (string.Equals(entityType, "cost", StringComparison.OrdinalIgnoreCase))
        {
            ignore.Add("statusHistory");
        }

        var keys = oldDoc.Names.Concat(newDoc.Names).Distinct(StringComparer.OrdinalIgnoreCase);
        var changed = new List<string>();

        foreach (var key in keys)
        {
            if (ignore.Contains(key)) continue;
            var hasOld = oldDoc.TryGetValue(key, out var oldVal);
            var hasNew = newDoc.TryGetValue(key, out var newVal);

            if (!hasOld || !hasNew)
            {
                changed.Add(key);
                continue;
            }

            if (!BsonValueEquals(oldVal, newVal))
            {
                changed.Add(key);
            }
        }

        return changed;
    }

    private static bool BsonValueEquals(BsonValue a, BsonValue b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.BsonType != b.BsonType) return a.ToJson() == b.ToJson();
        return a.Equals(b);
    }
}
