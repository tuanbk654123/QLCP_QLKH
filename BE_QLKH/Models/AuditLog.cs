using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

[BsonIgnoreExtraElements]
public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("entity_legacy_id")]
    public int EntityLegacyId { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("actor_user_id")]
    public int ActorUserId { get; set; }

    [BsonElement("actor_full_name")]
    public string ActorFullName { get; set; } = string.Empty;

    [BsonElement("actor_position")]
    public string ActorPosition { get; set; } = string.Empty;

    [BsonElement("actor_role")]
    public string ActorRole { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [BsonElement("old_data")]
    public BsonDocument? OldData { get; set; }

    [BsonElement("new_data")]
    public BsonDocument? NewData { get; set; }

    [BsonElement("changed_fields")]
    public List<string> ChangedFields { get; set; } = new();
}

