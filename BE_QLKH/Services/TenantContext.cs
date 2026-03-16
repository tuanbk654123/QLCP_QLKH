using System.Security.Claims;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BE_QLKH.Services;

public static class TenantContext
{
    public static string? GetCompanyId(ClaimsPrincipal user)
    {
        var companyId = user.FindFirst("company_id")?.Value;
        if (string.IsNullOrWhiteSpace(companyId)) return null;
        return companyId;
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
}
