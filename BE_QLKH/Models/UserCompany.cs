using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class UserCompany
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("user_legacy_id")]
    public int UserLegacyId { get; set; }

    [BsonElement("company_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyId { get; set; } = string.Empty;

    [BsonElement("role_code")]
    public string? RoleCode { get; set; }

    [BsonElement("is_default")]
    public bool IsDefault { get; set; }

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}
