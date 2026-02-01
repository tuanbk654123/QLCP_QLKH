using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BE_QLKH.Models;

[BsonIgnoreExtraElements]
public class Cost
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("legacy_id")]
    public int LegacyId { get; set; }

    [BsonElement("requester")]
    public string Requester { get; set; } = string.Empty;

    [BsonElement("department")]
    public string Department { get; set; } = string.Empty;

    [BsonElement("request_date")]
    public string RequestDate { get; set; } = string.Empty;

    [BsonElement("project_code")]
    public string ProjectCode { get; set; } = string.Empty;

    [BsonElement("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;

    [BsonElement("transaction_object")]
    public string TransactionObject { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("amount_before_tax")]
    public decimal AmountBeforeTax { get; set; }

    [BsonElement("tax_rate")]
    public string? TaxRate { get; set; }

    [BsonElement("total_amount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("payment_method")]
    public string? PaymentMethod { get; set; }

    [BsonElement("bank")]
    public string? Bank { get; set; }

    [BsonElement("account_number")]
    public string? AccountNumber { get; set; }

    [BsonElement("voucher_type")]
    public string? VoucherType { get; set; }

    [BsonElement("voucher_number")]
    public string? VoucherNumber { get; set; }

    [BsonElement("voucher_date")]
    public string? VoucherDate { get; set; }

    [BsonElement("transaction_date")]
    public string? TransactionDate { get; set; }

    [BsonElement("attachment")]
    public string? Attachment { get; set; }
    
    [BsonElement("attachments")]
    public List<AttachmentItem> Attachments { get; set; } = new();

    [BsonElement("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    [BsonElement("rejection_reason")]
    public string? RejectionReason { get; set; }
 
    [BsonElement("note")]
    public string? Note { get; set; }

    [BsonElement("created_by_user_id")]
    public int CreatedByUserId { get; set; }

    [BsonElement("status_history")]
    public List<CostStatusHistory> StatusHistory { get; set; } = new();

    [BsonElement("notification_recipients")]
    public List<int> NotificationRecipients { get; set; } = new();
}

public class CostStatusHistory
{
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("changed_by_user_id")]
    public int ChangedByUserId { get; set; }

    [BsonElement("changed_at")]
    public string ChangedAt { get; set; } = string.Empty;

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;
}

public class AttachmentItem
{
    [BsonElement("path")]
    public string Path { get; set; } = string.Empty;
    
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
}
