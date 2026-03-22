using System.Security.Claims;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BE_QLKH.Services;

public static class TenantContext
{
    public const string AllCompaniesScope = "all";

    public static string? GetRoleCode(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    public static bool IsGlobalViewer(ClaimsPrincipal user)
    {
        var role = GetRoleCode(user);
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "assistant_ceo", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetCompanyId(ClaimsPrincipal user)
    {
        var companyId = user.FindFirst("company_id")?.Value;
        if (string.IsNullOrWhiteSpace(companyId)) return null;
        return companyId;
    }

    public static string GetCompanyScope(ClaimsPrincipal user)
    {
        var scope = user.FindFirst("company_scope")?.Value;
        return string.IsNullOrWhiteSpace(scope) ? "single" : scope;
    }

    public static List<string> GetCompanyIds(ClaimsPrincipal user)
    {
        var raw = user.FindFirst("company_ids")?.Value;
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
    }

    public static string GetCompanyIdOrThrow(ClaimsPrincipal user)
    {
        var companyId = GetCompanyId(user);
        if (string.IsNullOrWhiteSpace(companyId)) throw new InvalidOperationException("Missing company_id");
        return companyId;
    }

    public static FilterDefinition<T> CompanyFilter<T>(string companyId)
    {
        if (ObjectId.TryParse(companyId, out var oid))
        {
            return Builders<T>.Filter.Or(
                Builders<T>.Filter.Eq("company_id", oid),
                Builders<T>.Filter.Eq("company_id", companyId)
            );
        }

        return Builders<T>.Filter.Eq("company_id", companyId);
    }

    public static FilterDefinition<T> ScopeFilter<T>(ClaimsPrincipal user)
    {
        var scope = GetCompanyScope(user);
        if (string.Equals(scope, AllCompaniesScope, StringComparison.OrdinalIgnoreCase))
        {
            var ids = GetCompanyIds(user);
            if (ids.Count > 0)
            {
                return CompanyIdsFilter<T>(ids);
            }
        }

        var companyId = GetCompanyIdOrThrow(user);
        return CompanyFilter<T>(companyId);
    }

    public static FilterDefinition<T> CompanyIdsFilter<T>(IEnumerable<string> companyIds)
    {
        var strIds = companyIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var objIds = strIds
            .Select(x => ObjectId.TryParse(x, out var oid) ? (ObjectId?)oid : null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        if (objIds.Count > 0)
        {
            return Builders<T>.Filter.Or(
                Builders<T>.Filter.In("company_id", objIds),
                Builders<T>.Filter.In("company_id", strIds)
            );
        }

        return Builders<T>.Filter.In("company_id", strIds);
    }

    public static async Task<int> GetNextLegacyIdAsync(
        IMongoDatabase db,
        string companyId,
        string entity,
        CancellationToken cancellationToken = default)
    {
        var counters = db.GetCollection<BsonDocument>("counters");
        var companyValue = ObjectId.TryParse(companyId, out var oid) ? (BsonValue)oid : companyId;
        var companyFilter = ObjectId.TryParse(companyId, out var companyOid)
            ? Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("company_id", companyOid),
                Builders<BsonDocument>.Filter.Eq("company_id", companyId)
            )
            : Builders<BsonDocument>.Filter.Eq("company_id", companyId);

        var filter = Builders<BsonDocument>.Filter.Eq("entity", entity) & companyFilter;
        var update = Builders<BsonDocument>.Update
            .Inc("seq", 1)
            .SetOnInsert("entity", entity)
            .SetOnInsert("company_id", companyValue);

        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var doc = await counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return doc.GetValue("seq", 0).ToInt32();
    }
}
