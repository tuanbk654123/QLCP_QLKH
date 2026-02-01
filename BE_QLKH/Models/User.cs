using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("employee_code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [BsonElement("employment_type")]
    public string EmploymentType { get; set; } = string.Empty;

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("dob")]
    public string Dob { get; set; } = string.Empty;

    [BsonElement("id_number")]
    public string IdNumber { get; set; } = string.Empty;

    [BsonElement("id_issued_date")]
    public string IdIssuedDate { get; set; } = string.Empty;

    [BsonElement("id_issued_place")]
    public string IdIssuedPlace { get; set; } = string.Empty;

    [BsonElement("personal_tax_code")]
    public string PersonalTaxCode { get; set; } = string.Empty;

    [BsonElement("bank_name")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("bank_account")]
    public string BankAccount { get; set; } = string.Empty;

    [BsonElement("social_insurance_number")]
    public string SocialInsuranceNumber { get; set; } = string.Empty;

    [BsonElement("health_insurance_number")]
    public string HealthInsuranceNumber { get; set; } = string.Empty;

    [BsonElement("role")]
    [JsonPropertyName("role")]
    public string RoleCode { get; set; } = string.Empty;

    [BsonElement("company")]
    public string Company { get; set; } = string.Empty;

    [BsonElement("department")]
    public string Department { get; set; } = string.Empty;

    [BsonElement("position")]
    public string Position { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("avatar")]
    [BsonIgnoreIfNull]
    public string? Avatar { get; set; }

    [BsonElement("join_date")]
    public string JoinDate { get; set; } = string.Empty;

    [BsonElement("salary")]
    public decimal Salary { get; set; }

    [BsonElement("contract_type")]
    public string ContractType { get; set; } = string.Empty;

    [BsonElement("contract_start_date")]
    public string ContractStartDate { get; set; } = string.Empty;

    [BsonElement("contract_end_date")]
    public string ContractEndDate { get; set; } = string.Empty;

    [BsonElement("work_location")]
    public string WorkLocation { get; set; } = string.Empty;

    [BsonElement("manager_name")]
    public string ManagerName { get; set; } = string.Empty;

    [BsonElement("manager_id")]
    [BsonIgnoreIfNull]
    public string? ManagerId { get; set; }

    [BsonElement("emergency_contact_name")]
    public string EmergencyContactName { get; set; } = string.Empty;

    [BsonElement("emergency_contact_phone")]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    [BsonElement("group")]
    public string Group { get; set; } = string.Empty;

    [BsonElement("data_scope")]
    public string DataScope { get; set; } = string.Empty;

    [BsonElement("direct_permission")]
    public bool DirectPermission { get; set; }

    [BsonElement("approve_right")]
    public bool ApproveRight { get; set; }

    [BsonElement("finance_view_right")]
    public bool FinanceViewRight { get; set; }

    [BsonElement("export_data_right")]
    public bool ExportDataRight { get; set; }

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [BsonElement("created_by")]
    public int CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [BsonElement("updated_by")]
    public int UpdatedBy { get; set; }

    [BsonElement("offboard_date")]
    public string OffboardDate { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;
}

