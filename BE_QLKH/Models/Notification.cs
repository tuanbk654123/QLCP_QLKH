using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("user_id")]
    public int UserId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // e.g., "CostApproval"

    [BsonElement("related_id")]
    public string RelatedId { get; set; } = string.Empty; // e.g., Cost LegacyId or ObjectId

    [BsonElement("is_read")]
    public bool IsRead { get; set; } = false;

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}
