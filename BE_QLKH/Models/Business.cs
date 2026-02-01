using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class Business
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("tax_code")]
    public string TaxCode { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("website")]
    public string Website { get; set; } = string.Empty;

    [BsonElement("industry")]
    public string Industry { get; set; } = string.Empty;

    [BsonElement("employee_count")]
    public int EmployeeCount { get; set; }

    [BsonElement("revenue")]
    public decimal Revenue { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("contact_person")]
    public string ContactPerson { get; set; } = string.Empty;

    [BsonElement("contact_phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [BsonElement("established_date")]
    public string EstablishedDate { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;
}

