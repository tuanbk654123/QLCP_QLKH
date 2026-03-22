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

    [BsonElement("company_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyId { get; set; } = string.Empty;

    [BsonElement("schema_version")]
    public string? SchemaVersion { get; set; }

    [BsonElement("name")]
    public string? Name { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("company")]
    public string? Company { get; set; }

    [BsonElement("tax_code")]
    public string? TaxCode { get; set; }

    [BsonElement("representative_name")]
    public string? RepresentativeName { get; set; }

    [BsonElement("representative_position")]
    public string? RepresentativePosition { get; set; }

    [BsonElement("representative_phone")]
    public string? RepresentativePhone { get; set; }

    [BsonElement("id_number")]
    public string? IdNumber { get; set; }

    [BsonElement("business_needs")]
    public string? BusinessNeeds { get; set; }

    [BsonElement("need_detail")]
    public string? NeedDetail { get; set; }

    [BsonElement("business_scale")]
    public string? BusinessScale { get; set; }

    [BsonElement("business_industry")]
    public string? BusinessIndustry { get; set; }

    [BsonElement("copyright_status")]
    public string? CopyrightStatus { get; set; }

    [BsonElement("trademark_status")]
    public string? TrademarkStatus { get; set; }

    [BsonElement("patent_status")]
    public string? PatentStatus { get; set; }

    [BsonElement("industrial_design")]
    public string? IndustrialDesign { get; set; }

    [BsonElement("contact_person")]
    public string? ContactPerson { get; set; }

    [BsonElement("contact_phone")]
    public string? ContactPhone { get; set; }

    [BsonElement("contact_email")]
    public string? ContactEmail { get; set; }

    [BsonElement("contract_status")]
    public string? ContractStatus { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; }

    [BsonElement("total_orders")]
    public int? TotalOrders { get; set; }

    [BsonElement("total_revenue")]
    public decimal? TotalRevenue { get; set; }

    [BsonElement("join_date")]
    public string? JoinDate { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("products_services")]
    public string? ProductsServices { get; set; }

    [BsonElement("product_link")]
    public string? ProductLink { get; set; }

    [BsonElement("owner_user_id")]
    public int? OwnerUserId { get; set; }

    [BsonElement("lifecycle_status")]
    public string? LifecycleStatus { get; set; }

    [BsonElement("tags")]
    public List<string>? Tags { get; set; }

    [BsonElement("ip_group")]
    public string? IpGroup { get; set; }

    [BsonElement("brand_name")]
    public string? BrandName { get; set; }

    [BsonElement("owner")]
    public string? Owner { get; set; }

    [BsonElement("protection_territory")]
    public string? ProtectionTerritory { get; set; }

    [BsonElement("consulting_status")]
    public string? ConsultingStatus { get; set; }

    [BsonElement("filing_status")]
    public string? FilingStatus { get; set; }

    [BsonElement("filing_date")]
    public string? FilingDate { get; set; }

    [BsonElement("application_code")]
    public string? ApplicationCode { get; set; }

    [BsonElement("issue_date")]
    public string? IssueDate { get; set; }

    [BsonElement("expiry_date")]
    public string? ExpiryDate { get; set; }

    [BsonElement("processing_deadline")]
    public string? ProcessingDeadline { get; set; }

    [BsonElement("renewal_cycle")]
    public string? RenewalCycle { get; set; }

    [BsonElement("renewal_date")]
    public string? RenewalDate { get; set; }

    [BsonElement("reminder_date")]
    public string? ReminderDate { get; set; }

    [BsonElement("reminder_status")]
    public string? ReminderStatus { get; set; }

    [BsonElement("document_link")]
    public string? DocumentLink { get; set; }

    [BsonElement("authorization")]
    public string? Authorization { get; set; }

    [BsonElement("application_review_status")]
    public string? ApplicationReviewStatus { get; set; }

    [BsonElement("priority")]
    public string? Priority { get; set; }

    [BsonElement("contract_paid")]
    public string? ContractPaid { get; set; }

    [BsonElement("contract_number")]
    public string? ContractNumber { get; set; }

    [BsonElement("contract_value")]
    public decimal? ContractValue { get; set; }

    [BsonElement("state_fee")]
    public decimal? StateFee { get; set; }

    [BsonElement("additional_fee")]
    public decimal? AdditionalFee { get; set; }

    [BsonElement("start_date")]
    public string? StartDate { get; set; }

    [BsonElement("end_date")]
    public string? EndDate { get; set; }

    [BsonElement("implementation_days")]
    public int? ImplementationDays { get; set; }

    [BsonElement("potential_level")]
    public string? PotentialLevel { get; set; }

    [BsonElement("source_classification")]
    public string? SourceClassification { get; set; }

    [BsonElement("nsnn_source")]
    public string? NsnnSource { get; set; }

    [BsonElement("created_at")]
    public string? CreatedAt { get; set; }

    [BsonElement("created_by")]
    public int CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public string? UpdatedAt { get; set; }

    [BsonElement("updated_by")]
    public int UpdatedBy { get; set; }
}

