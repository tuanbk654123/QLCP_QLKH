using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

[BsonIgnoreExtraElements]
public class ProjectModule
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("company_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyId { get; set; } = string.Empty;

    [BsonElement("project_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("project_legacy_id")]
    public int ProjectLegacyId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("owner_user_id")]
    public int? OwnerUserId { get; set; }

    [BsonElement("start_date")]
    public string? StartDate { get; set; }

    [BsonElement("end_date")]
    public string? EndDate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [BsonElement("progress")]
    public int Progress { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } = "MEDIUM";

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("task_count")]
    public int TaskCount { get; set; }

    [BsonElement("task_done_count")]
    public int TaskDoneCount { get; set; }

    [BsonElement("weighted_progress_sum")]
    public int WeightedProgressSum { get; set; }

    [BsonElement("weight_sum")]
    public int WeightSum { get; set; }

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [BsonElement("created_by")]
    public int CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [BsonElement("updated_by")]
    public int UpdatedBy { get; set; }
}

