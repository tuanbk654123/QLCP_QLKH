using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class Contract
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("contract_number")]
    public string ContractNumber { get; set; } = string.Empty;

    [BsonElement("contract_type")]
    public string ContractType { get; set; } = string.Empty;

    [BsonElement("party_a")]
    public ContractParty PartyA { get; set; } = new();

    [BsonElement("party_b")]
    public ContractParty PartyB { get; set; } = new();

    [BsonElement("contract_date")]
    public string ContractDate { get; set; } = string.Empty;

    [BsonElement("effective_date")]
    public string EffectiveDate { get; set; } = string.Empty;

    [BsonElement("expiry_date")]
    public string ExpiryDate { get; set; } = string.Empty;

    [BsonElement("salary")]
    public decimal Salary { get; set; }

    [BsonElement("working_hours")]
    public string WorkingHours { get; set; } = string.Empty;

    [BsonElement("job_description")]
    public string JobDescription { get; set; } = string.Empty;

    [BsonElement("service_description")]
    public string ServiceDescription { get; set; } = string.Empty;

    [BsonElement("service_fee")]
    public decimal ServiceFee { get; set; }

    [BsonElement("payment_terms")]
    public string PaymentTerms { get; set; } = string.Empty;

    [BsonElement("product_description")]
    public string ProductDescription { get; set; } = string.Empty;

    [BsonElement("total_amount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("delivery_terms")]
    public string DeliveryTerms { get; set; } = string.Empty;

    [BsonElement("terms")]
    public string Terms { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;
}

public class ContractParty
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("tax_code")]
    public string TaxCode { get; set; } = string.Empty;

    [BsonElement("id_card")]
    public string IdCard { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("representative")]
    public string Representative { get; set; } = string.Empty;

    [BsonElement("position")]
    public string Position { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;
}

