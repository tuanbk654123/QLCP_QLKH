using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class FieldPermission
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("role_code")]
    public string RoleCode { get; set; } = string.Empty;

    [BsonElement("module_code")]
    public string ModuleCode { get; set; } = string.Empty;

    [BsonElement("field_code")]
    public string FieldCode { get; set; } = string.Empty;

    [BsonElement("permission_level")]
    public string PermissionLevel { get; set; } = "R";
}

