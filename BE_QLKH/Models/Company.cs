using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("domain")]
    public string? Domain { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [BsonElement("created_by")]
    public int CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [BsonElement("updated_by")]
    public int UpdatedBy { get; set; }
}

