using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

[BsonIgnoreExtraElements]
public class ProjectTaskActivity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("company_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyId { get; set; } = string.Empty;

    [BsonElement("task_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TaskId { get; set; } = string.Empty;

    [BsonElement("task_legacy_id")]
    public int TaskLegacyId { get; set; }

    [BsonElement("actor_user_id")]
    public int? ActorUserId { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("from_value")]
    public string? FromValue { get; set; }

    [BsonElement("to_value")]
    public string? ToValue { get; set; }

    [BsonElement("meta_json")]
    public string? MetaJson { get; set; }

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

