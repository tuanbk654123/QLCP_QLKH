using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class FieldDef
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("module_code")]
    public string ModuleCode { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("group_code")]
    public string GroupCode { get; set; } = string.Empty;

    [BsonElement("group_label")]
    public string GroupLabel { get; set; } = string.Empty;

    [BsonElement("order_index")]
    public int OrderIndex { get; set; }

    [BsonElement("field_type")]
    public string FieldType { get; set; } = "normal";
}

