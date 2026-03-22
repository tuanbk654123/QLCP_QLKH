using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

[BsonIgnoreExtraElements]
public class ProjectTask
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

    [BsonElement("module_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ModuleId { get; set; } = string.Empty;

    [BsonElement("module_legacy_id")]
    public int ModuleLegacyId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("assignee_user_id")]
    public int? AssigneeUserId { get; set; }

    [BsonElement("assigner_user_id")]
    public int? AssignerUserId { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "NOT_STARTED";

    [BsonElement("priority")]
    public string Priority { get; set; } = "MEDIUM";

    [BsonElement("progress")]
    public int Progress { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("start_date")]
    public string? StartDate { get; set; }

    [BsonElement("end_date")]
    public string? EndDate { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("estimated_minutes")]
    public int? EstimatedMinutes { get; set; }

    [BsonElement("actual_minutes")]
    public int? ActualMinutes { get; set; }

    [BsonElement("deadline_overdue")]
    public bool DeadlineOverdue { get; set; }

    [BsonElement("attachments")]
    public List<ProjectTaskAttachment> Attachments { get; set; } = new();

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [BsonElement("created_by")]
    public int CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [BsonElement("updated_by")]
    public int UpdatedBy { get; set; }
}

public class ProjectTaskAttachment
{
    [BsonElement("type")]
    public string Type { get; set; } = "url";

    [BsonElement("name")]
    public string? Name { get; set; }

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("size")]
    public int? Size { get; set; }

    [BsonElement("mime_type")]
    public string? MimeType { get; set; }
}

