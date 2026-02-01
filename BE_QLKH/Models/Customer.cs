using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("company")]
    public string Company { get; set; } = string.Empty;

    [BsonElement("tax_code")]
    public string TaxCode { get; set; } = string.Empty;

    [BsonElement("representative_name")]
    public string RepresentativeName { get; set; } = string.Empty;

    [BsonElement("representative_position")]
    public string RepresentativePosition { get; set; } = string.Empty;

    [BsonElement("representative_phone")]
    public string RepresentativePhone { get; set; } = string.Empty;

    [BsonElement("business_needs")]
    public string BusinessNeeds { get; set; } = string.Empty;

    [BsonElement("business_scale")]
    public string BusinessScale { get; set; } = string.Empty;

    [BsonElement("business_industry")]
    public string BusinessIndustry { get; set; } = string.Empty;

    [BsonElement("copyright_status")]
    public string CopyrightStatus { get; set; } = string.Empty;

    [BsonElement("trademark_status")]
    public string TrademarkStatus { get; set; } = string.Empty;

    [BsonElement("patent_status")]
    public string PatentStatus { get; set; } = string.Empty;

    [BsonElement("industrial_design")]
    public string IndustrialDesign { get; set; } = string.Empty;

    [BsonElement("contract_status")]
    public string ContractStatus { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("total_orders")]
    public int TotalOrders { get; set; }

    [BsonElement("total_revenue")]
    public decimal TotalRevenue { get; set; }

    [BsonElement("join_date")]
    public string JoinDate { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("products_services")]
    public string ProductsServices { get; set; } = string.Empty;

    [BsonElement("ip_group")]
    public string IpGroup { get; set; } = string.Empty;

    [BsonElement("consulting_status")]
    public string ConsultingStatus { get; set; } = string.Empty;

    [BsonElement("filing_status")]
    public string FilingStatus { get; set; } = string.Empty;

    [BsonElement("document_link")]
    public string DocumentLink { get; set; } = string.Empty;

    [BsonElement("authorization")]
    public string Authorization { get; set; } = string.Empty;

    [BsonElement("application_review_status")]
    public string ApplicationReviewStatus { get; set; } = string.Empty;

    [BsonElement("priority")]
    public string Priority { get; set; } = string.Empty;

    [BsonElement("contract_paid")]
    public string ContractPaid { get; set; } = string.Empty;

    [BsonElement("contract_value")]
    public decimal ContractValue { get; set; }

    [BsonElement("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [BsonElement("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [BsonElement("implementation_days")]
    public int ImplementationDays { get; set; }

    [BsonElement("potential_level")]
    public string PotentialLevel { get; set; } = string.Empty;

    [BsonElement("source_classification")]
    public string SourceClassification { get; set; } = string.Empty;

    [BsonElement("nsnn_source")]
    public string NsnnSource { get; set; } = string.Empty;
}

