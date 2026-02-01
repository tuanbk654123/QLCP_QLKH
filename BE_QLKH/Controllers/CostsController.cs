using BE_QLKH.Hubs;
using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CostsController : ControllerBase
{
    private readonly IMongoCollection<Cost> _costs;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Notification> _notifications;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<CostsController> _logger;
    private readonly IEmailService _emailService;

    public CostsController(
        IMongoClient client, 
        IOptions<MongoDbSettings> options,
        IHubContext<NotificationsHub> hubContext,
        ILogger<CostsController> logger,
        IEmailService emailService)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _costs = db.GetCollection<Cost>("costs");
        _users = db.GetCollection<User>("users");
        _notifications = db.GetCollection<Notification>("notifications");
        _hubContext = hubContext;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetCosts(
        [FromQuery] string? search, 
        [FromQuery] int page = 1,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        if (page < 1) page = 1;
        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var builder = Builders<Cost>.Filter;
        var filter = builder.Empty;

        // Role-Based Access Control (RBAC) for Visibility
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // This is the ObjectId string usually, but let's check legacy_id claim
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;

        if (int.TryParse(legacyIdStr, out int currentUserId))
        {
            // Define roles that can view ALL costs
            var viewAllRoles = new[] { "admin", "director", "giam_doc", "manager", "ip_manager", "quan_ly", "accountant", "ke_toan", "marketing_sales", "sales" };
            
            if (!viewAllRoles.Contains(userRole))
            {
                // Regular employees only see their own costs
                filter &= builder.Eq(c => c.CreatedByUserId, currentUserId);
            }
        }

        // Global Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search), "i");
            filter &= builder.Or(
                builder.Regex(c => c.Content, searchRegex),
                builder.Regex(c => c.Requester, searchRegex),
                builder.Regex(c => c.VoucherNumber, searchRegex),
                builder.Regex(c => c.Description, searchRegex),
                builder.Regex(c => c.ProjectCode, searchRegex)
            );
        }

        // Column Filters via Reflection
        var properties = typeof(Cost).GetProperties();
        foreach (var query in Request.Query)
        {
            var key = query.Key;
            var value = query.Value.ToString();
            
            if (string.IsNullOrEmpty(value)) continue;
            if (new[] { "search", "page", "sortfield", "sortorder", "limit" }.Contains(key.ToLower())) continue;

            var prop = properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                var bsonAttr = prop.GetCustomAttributes(typeof(BsonElementAttribute), false)
                    .FirstOrDefault() as BsonElementAttribute;
                var dbField = bsonAttr?.ElementName ?? prop.Name;
                
                if (prop.PropertyType == typeof(string))
                {
                    filter &= builder.Regex(dbField, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(value), "i"));
                }
                else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                {
                    if (int.TryParse(value, out int intVal))
                    {
                        filter &= builder.Eq(dbField, intVal);
                    }
                }
                else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                {
                     if (decimal.TryParse(value, out decimal decVal))
                    {
                        filter &= builder.Eq(dbField, decVal);
                    }
                }
                 else 
                {
                     filter &= builder.Regex(dbField, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(value), "i"));
                }
            }
        }

        // Sorting
        SortDefinition<Cost> sort = Builders<Cost>.Sort.Descending(c => c.LegacyId);
        if (!string.IsNullOrEmpty(sortField))
        {
            var prop = properties.FirstOrDefault(p => p.Name.Equals(sortField, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                var bsonAttr = prop.GetCustomAttributes(typeof(BsonElementAttribute), false)
                    .FirstOrDefault() as BsonElementAttribute;
                var dbField = bsonAttr?.ElementName ?? prop.Name;
                
                if (sortOrder?.ToLower() == "asc" || sortOrder?.ToLower() == "ascend")
                    sort = Builders<Cost>.Sort.Ascending(dbField);
                else
                    sort = Builders<Cost>.Sort.Descending(dbField);
            }
        }

        var total = await _costs.CountDocumentsAsync(filter);
        var costs = await _costs
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var result = costs.Select(c => new
        {
            id = c.LegacyId,
            requester = c.Requester,
            department = c.Department,
            requestDate = c.RequestDate,
            projectCode = c.ProjectCode,
            transactionType = c.TransactionType,
            transactionObject = c.TransactionObject,
            transactionDate = c.TransactionDate,
            content = c.Content,
            description = c.Description,
            amountBeforeTax = c.AmountBeforeTax,
            taxRate = c.TaxRate,
            totalAmount = c.TotalAmount,
            paymentMethod = c.PaymentMethod,
            bank = c.Bank,
            accountNumber = c.AccountNumber,
            voucherType = c.VoucherType,
            voucherNumber = c.VoucherNumber,
            voucherDate = c.VoucherDate,
            attachment = c.Attachment,
            attachments = c.Attachments,
            paymentStatus = c.PaymentStatus,
            rejectionReason = c.RejectionReason,
            note = c.Note,
            statusHistory = c.StatusHistory,
            createdByUserId = c.CreatedByUserId
        });

        return Ok(new
        {
            costs = result,
            costCount = total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetCostByLegacyId(int id)
    {
        var cost = await _costs.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (cost == null) return NotFound(new { message = "Cost not found" });

        return Ok(new
        {
            id = cost.LegacyId,
            requester = cost.Requester,
            department = cost.Department,
            requestDate = cost.RequestDate,
            projectCode = cost.ProjectCode,
            transactionType = cost.TransactionType,
            transactionObject = cost.TransactionObject,
            transactionDate = cost.TransactionDate,
            content = cost.Content,
            description = cost.Description,
            amountBeforeTax = cost.AmountBeforeTax,
            taxRate = cost.TaxRate,
            totalAmount = cost.TotalAmount,
            paymentMethod = cost.PaymentMethod,
            bank = cost.Bank,
            accountNumber = cost.AccountNumber,
            voucherType = cost.VoucherType,
            voucherNumber = cost.VoucherNumber,
            voucherDate = cost.VoucherDate,
            attachment = cost.Attachment,
            attachments = cost.Attachments,
            paymentStatus = cost.PaymentStatus,
            rejectionReason = cost.RejectionReason,
            note = cost.Note,
            statusHistory = cost.StatusHistory,
            createdByUserId = cost.CreatedByUserId
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateCost([FromBody] Cost input)
    {
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var userName = User.Identity?.Name ?? "Unknown";

        input.Id = ObjectId.GenerateNewId().ToString();

        var maxLegacyId = await _costs.Find(_ => true)
            .SortByDescending(c => c.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;
        input.CreatedByUserId = userId;
        input.PaymentStatus = "Đợi duyệt";
        
        input.StatusHistory = new List<CostStatusHistory>
        {
            new CostStatusHistory
            {
                Status = "Đợi duyệt",
                ChangedByUserId = userId,
                ChangedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Note = "Tạo mới và gửi duyệt"
            }
        };

        await _costs.InsertOneAsync(input);

        // Notify Selected Recipients
        HashSet<int> notifiedUserIds = new HashSet<int>();

        if (input.NotificationRecipients != null && input.NotificationRecipients.Count > 0)
        {
            foreach (var recipientId in input.NotificationRecipients)
            {
                 if (notifiedUserIds.Contains(recipientId)) continue;
                 
                 await CreateAndSendNotification(recipientId, "Phiếu chi mới cần duyệt", 
                    $"{userName} đã tạo phiếu chi #{input.LegacyId}. Vui lòng duyệt.", "CostApproval", input.LegacyId.ToString());
                 notifiedUserIds.Add(recipientId);
            }
        }
        else
        {
            // Fallback: Notify Specific Manager if available
            var creator = await _users.Find(u => u.LegacyId == userId).FirstOrDefaultAsync();
            bool managerNotified = false;

            if (creator != null && !string.IsNullOrEmpty(creator.ManagerId) && int.TryParse(creator.ManagerId, out int managerId))
            {
                 if (!notifiedUserIds.Contains(managerId))
                 {
                     await CreateAndSendNotification(managerId, "Phiếu chi mới cần duyệt", 
                        $"{userName} đã tạo phiếu chi #{input.LegacyId}. Vui lòng duyệt.", "CostApproval", input.LegacyId.ToString());
                     notifiedUserIds.Add(managerId);
                 }
                 managerNotified = true;
            }

            // If no specific manager, notify all managers
            if (!managerNotified)
            {
                await SendNotificationToRole("ip_manager", "Phiếu chi mới cần duyệt", 
                    $"{userName} đã tạo phiếu chi #{input.LegacyId}. Vui lòng duyệt.", "CostApproval", input.LegacyId.ToString(), notifiedUserIds);
            }
            
            await SendNotificationToRole("admin", "Phiếu chi mới cần duyệt", 
                $"{userName} đã tạo phiếu chi #{input.LegacyId}. Vui lòng duyệt.", "CostApproval", input.LegacyId.ToString(), notifiedUserIds);
        }

        return Ok(new { message = "Tạo phiếu thành công", id = input.LegacyId });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateCost(int id, [FromBody] Cost input)
    {
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userName = User.Identity?.Name ?? "Unknown";

        var cost = await _costs.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (cost == null) return NotFound(new { message = "Cost not found" });

        // RBAC: Check if user can edit
        bool canEdit = false;
        
        // Admin can always edit
        if (userRole == "admin") canEdit = true;
        
        // Creator can edit if Draft or Cancelled
        else if (cost.CreatedByUserId == userId && (cost.PaymentStatus == "Đợi duyệt" || cost.PaymentStatus == "Huỷ")) canEdit = true;
        
        // Manager can edit if Draft (to help?) or Manager Approval stage
        else if ((userRole == "manager" || userRole == "ip_manager" || userRole == "quan_ly") && 
                 (cost.PaymentStatus == "Đợi duyệt" || cost.PaymentStatus == "Quản lý duyệt")) canEdit = true;
                 
        // Director can edit if Manager Approval (Skip-level) or Director Approval stage
        else if ((userRole == "director" || userRole == "giam_doc") && 
                 (cost.PaymentStatus == "Đợi duyệt" || cost.PaymentStatus == "Quản lý duyệt" || cost.PaymentStatus == "Giám đốc duyệt")) canEdit = true;
                 
        // Accountant can edit if Director Approval (Pre-payment)
        else if ((userRole == "accountant" || userRole == "ke_toan") && cost.PaymentStatus == "Giám đốc duyệt") canEdit = true;

        if (!canEdit)
        {
            return StatusCode(403, new { message = "Bạn không có quyền chỉnh sửa phiếu này ở trạng thái hiện tại" });
        }
        
        // FIX: If input.PaymentStatus is null/empty (e.g. removed by frontend), preserve existing status
        if (string.IsNullOrEmpty(input.PaymentStatus))
        {
            input.PaymentStatus = cost.PaymentStatus;
            
            // Self-healing: If status is still null/empty (corrupted data), recover from history
            if (string.IsNullOrEmpty(input.PaymentStatus))
            {
                var lastValid = cost.StatusHistory.LastOrDefault(h => !string.IsNullOrEmpty(h.Status));
                input.PaymentStatus = lastValid?.Status ?? "Đợi duyệt";
            }
        }

        // Detect status change and log history
        if (input.PaymentStatus != cost.PaymentStatus)
        {
             cost.StatusHistory.Add(new CostStatusHistory
             {
                 Status = input.PaymentStatus,
                 ChangedByUserId = userId,
                 ChangedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                 Note = !string.IsNullOrEmpty(input.RejectionReason) && input.PaymentStatus == "Huỷ" 
                        ? $"Từ chối: {input.RejectionReason}" 
                        : $"Cập nhật trạng thái: {input.PaymentStatus}"
             });
        }

        input.Id = cost.Id;
        input.LegacyId = cost.LegacyId;
        input.CreatedByUserId = cost.CreatedByUserId;
        input.StatusHistory = cost.StatusHistory;
        
        // Preserve fields that are not in the form (prevent data loss)
        if (string.IsNullOrEmpty(input.Requester)) input.Requester = cost.Requester;
        if (string.IsNullOrEmpty(input.Department)) input.Department = cost.Department;
        if (string.IsNullOrEmpty(input.ProjectCode)) input.ProjectCode = cost.ProjectCode;
        if (string.IsNullOrEmpty(input.TransactionType)) input.TransactionType = cost.TransactionType;
        if (string.IsNullOrEmpty(input.TransactionObject)) input.TransactionObject = cost.TransactionObject;
        
        // Allow PaymentStatus, RejectionReason, Approver* fields to be updated from input

        await _costs.ReplaceOneAsync(c => c.Id == cost.Id, input);

        return Ok(new { message = "Cập nhật thành công", id = input.LegacyId });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteCost(int id)
    {
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var cost = await _costs.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (cost == null) return NotFound(new { message = "Cost not found" });

        // RBAC: Check if user can delete
        bool canDelete = false;

        if (userRole == "admin") canDelete = true;
        // Creator can delete only if Draft or Cancelled
        else if (cost.CreatedByUserId == userId && (cost.PaymentStatus == "Đợi duyệt" || cost.PaymentStatus == "Huỷ" || cost.PaymentStatus == "Từ chối")) canDelete = true;
        
        if (!canDelete)
        {
             return StatusCode(403, new { message = "Bạn không có quyền xóa phiếu này (chỉ xóa được phiếu nháp hoặc phiếu đã bị hủy)" });
        }

        var result = await _costs.DeleteOneAsync(c => c.LegacyId == id);
        return Ok(new { message = "Cost deleted" });
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<object>> ApproveCost(int id, [FromBody] JsonElement body)
    {
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userName = User.Identity?.Name ?? "Unknown";

        List<int> extraRecipients = new List<int>();
        if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("notificationRecipients", out var recipientsProp))
        {
            if (recipientsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in recipientsProp.EnumerateArray())
                {
                    if (item.TryGetInt32(out int rid)) extraRecipients.Add(rid);
                }
            }
        }

        var cost = await _costs.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (cost == null) return NotFound(new { message = "Cost not found" });

        // Get Requester Info for hierarchy lookup
        var requester = await _users.Find(u => u.LegacyId == cost.CreatedByUserId).FirstOrDefaultAsync();

        string nextStatus = "";
        List<int> userIdsToNotify = new List<int>();
        string roleToNotifyFallback = "";
        string notificationTitle = "";
        string notificationMsg = "";

        // Workflow Logic
        if (cost.PaymentStatus == "Đợi duyệt")
        {
            if (userRole == "ip_manager" || userRole == "quan_ly" || userRole == "manager" || userRole == "admin")
            {
                nextStatus = "Quản lý duyệt";
                // cost.ApproverManager = "Đã duyệt"; // Removed
                
                // Determine Director to notify
                // Case 1: Requester is Manager -> Notify their Manager (Director)
                // Case 2: Requester is Employee -> Notify their Manager's Manager (Director)
                
                if (requester != null)
                {
                    if (requester.RoleCode == "ip_manager" || requester.RoleCode == "quan_ly")
                    {
                        if (!string.IsNullOrEmpty(requester.ManagerId) && int.TryParse(requester.ManagerId, out int directManagerId))
                        {
                            userIdsToNotify.Add(directManagerId);
                        }
                    }
                    else // Assume Employee
                    {
                        if (!string.IsNullOrEmpty(requester.ManagerId) && int.TryParse(requester.ManagerId, out int managerId))
                        {
                            var managerUser = await _users.Find(u => u.LegacyId == managerId).FirstOrDefaultAsync();
                            if (managerUser != null && !string.IsNullOrEmpty(managerUser.ManagerId))
                            {
                                if (int.TryParse(managerUser.ManagerId, out int directorId))
                                {
                                    userIdsToNotify.Add(directorId);
                                }
                            }
                        }
                    }
                }

                if (userIdsToNotify.Count == 0) roleToNotifyFallback = "giam_doc";
                
                notificationTitle = "Phiếu chi đã được quản lý duyệt";
                notificationMsg = $"Quản lý {userName} đã duyệt phiếu chi #{id}. Chờ giám đốc duyệt.";
            }
            else if (userRole == "giam_doc" || userRole == "director")
            {
                 // Director approving at "Đợi duyệt" (Skip Manager or Direct Approval)
                 nextStatus = "Giám đốc duyệt";
                 roleToNotifyFallback = "ke_toan";
                 
                 notificationTitle = "Phiếu chi đã được giám đốc duyệt";
                 notificationMsg = $"Giám đốc {userName} đã duyệt phiếu chi #{id}. Chờ kế toán thanh toán.";
            }
            else return BadRequest(new { message = "Bạn không có quyền duyệt phiếu này" });
        }
        else if (cost.PaymentStatus == "Quản lý duyệt")
        {
             if (userRole == "giam_doc" || userRole == "director" || userRole == "admin")
            {
                nextStatus = "Giám đốc duyệt";
                // cost.ApproverDirector = "Đã duyệt"; // Removed
                
                roleToNotifyFallback = "ke_toan"; // Accountants are usually a pool
                
                notificationTitle = "Phiếu chi đã được giám đốc duyệt";
                notificationMsg = $"Giám đốc {userName} đã duyệt phiếu chi #{id}. Chờ kế toán thanh toán.";
            }
             else return BadRequest(new { message = "Bạn không có quyền duyệt phiếu này" });
        }
        else if (cost.PaymentStatus == "Giám đốc duyệt")
        {
             if (userRole == "ke_toan" || userRole == "accountant" || userRole == "admin")
            {
                nextStatus = "Đã thanh toán";
                // cost.AccountantReview = "Đã duyệt"; // Removed
                
                notificationTitle = "Phiếu chi đã được thanh toán";
                notificationMsg = $"Kế toán {userName} đã xác nhận thanh toán phiếu chi #{id}.";
            }
             else return BadRequest(new { message = "Bạn không có quyền duyệt phiếu này" });
        }
        else
        {
            return BadRequest(new { message = "Trạng thái phiếu không hợp lệ để duyệt" });
        }

        cost.PaymentStatus = nextStatus;
        cost.StatusHistory.Add(new CostStatusHistory
        {
            Status = nextStatus,
            ChangedByUserId = userId,
            ChangedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Note = $"Được duyệt bởi {userName}"
        });

        await _costs.ReplaceOneAsync(c => c.Id == cost.Id, cost);

        // Send Notifications
        // 1. To Specific Users (Hierarchy)
        if (userIdsToNotify.Count > 0)
        {
            foreach(var uid in userIdsToNotify)
            {
                await CreateAndSendNotification(uid, notificationTitle, notificationMsg, "CostApproval", id.ToString());
            }
        }

        // 2. To Role (Fallback or Required)
        // If specific users notified, maybe skip role unless it's accountant?
        // Let's send to role if NO specific users found, OR if role is 'ke_toan' (pool)
        if (!string.IsNullOrEmpty(roleToNotifyFallback))
        {
            if (userIdsToNotify.Count == 0 || roleToNotifyFallback == "accountant" || roleToNotifyFallback == "ke_toan")
            {
                await SendNotificationToRole(roleToNotifyFallback, notificationTitle, notificationMsg, "CostApproval", id.ToString());
            }
        }
        
        // 3. Always notify Admin (for visibility)
        if (roleToNotifyFallback != "admin") 
             await SendNotificationToRole("admin", notificationTitle, notificationMsg, "CostApproval", id.ToString());

        // 4. Always notify Requester (if not self)
        if (cost.CreatedByUserId != userId)
        {
             await CreateAndSendNotification(cost.CreatedByUserId, notificationTitle, notificationMsg, "CostApproval", id.ToString());
        }

        // 5. Notify Extra Recipients (Manually selected)
        if (extraRecipients.Count > 0)
        {
            foreach (var recipientId in extraRecipients)
            {
                 // Avoid duplicate notification if already notified automatically
                 if (!userIdsToNotify.Contains(recipientId) && cost.CreatedByUserId != recipientId)
                 {
                      await CreateAndSendNotification(recipientId, notificationTitle, notificationMsg, "CostApproval", id.ToString());
                 }
            }
        }

        return Ok(new { message = "Duyệt thành công", status = nextStatus });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<object>> RejectCost(int id, [FromBody] JsonElement body)
    {
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();
        var userName = User.Identity?.Name ?? "Unknown";

        string reason = "";
        if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("reason", out var reasonProp))
        {
            reason = reasonProp.GetString() ?? "";
        }

        var cost = await _costs.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (cost == null) return NotFound(new { message = "Cost not found" });

        cost.PaymentStatus = "Từ chối";
        cost.RejectionReason = reason;
        cost.StatusHistory.Add(new CostStatusHistory
        {
            Status = "Từ chối",
            ChangedByUserId = userId,
            ChangedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Note = $"Từ chối: {reason}"
        });

        await _costs.ReplaceOneAsync(c => c.Id == cost.Id, cost);

        // Notify Requester
        await CreateAndSendNotification(cost.CreatedByUserId, "Phiếu chi bị từ chối", 
            $"{userName} đã từ chối phiếu chi #{id}. Lý do: {reason}", "CostApproval", id.ToString());
            
        // Also notify admin
        await SendNotificationToRole("admin", "Phiếu chi bị từ chối", 
             $"{userName} đã từ chối phiếu chi #{id}. Lý do: {reason}", "CostApproval", id.ToString());

        return Ok(new { message = "Đã từ chối phiếu" });
    }

    private async Task SendNotificationToRole(string roleCode, string title, string message, string type, string relatedId, HashSet<int>? notifiedUserIds = null)
    {
        var users = await _users.Find(u => u.RoleCode == roleCode).ToListAsync();
        foreach (var user in users)
        {
            if (notifiedUserIds != null)
            {
                if (notifiedUserIds.Contains(user.LegacyId)) continue;
                notifiedUserIds.Add(user.LegacyId);
            }
            
            await CreateAndSendNotification(user.LegacyId, title, message, type, relatedId);
        }
    }
    
    private async Task CreateAndSendNotification(int userId, string title, string message, string type, string relatedId)
    {
        var notif = new Notification
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedId = relatedId,
            IsRead = false,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        await _notifications.InsertOneAsync(notif);
        
        try 
        {
            await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notif);
        }
        catch 
        {
            // Ignore SignalR errors
        }

        // Send Email
        try
        {
            var user = await _users.Find(u => u.LegacyId == userId).FirstOrDefaultAsync();
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                Console.WriteLine($"Found user {user.Username} with email {user.Email}. Sending notification email...");
                
                string emailBody = message;
                
                // Try to find related Cost to enrich email content
                if (!string.IsNullOrEmpty(relatedId))
                {
                    Cost? relatedCost = null;
                    if (int.TryParse(relatedId, out int costLegacyId))
                    {
                        relatedCost = await _costs.Find(c => c.LegacyId == costLegacyId).FirstOrDefaultAsync();
                    }
                    else
                    {
                        relatedCost = await _costs.Find(c => c.Id == relatedId).FirstOrDefaultAsync();
                    }

                    if (relatedCost != null)
                    {
                        emailBody = $@"
                        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                            <p>{message}</p>
                            <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; border: 1px solid #ddd; margin-top: 20px;'>
                                <h3 style='margin-top: 0; color: #3498db;'>Thông tin phiếu chi</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; width: 150px;'>Mã phiếu:</td>
                                        <td>{relatedCost.VoucherNumber ?? relatedCost.LegacyId.ToString()}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Người yêu cầu:</td>
                                        <td>{relatedCost.Requester}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Bộ phận:</td>
                                        <td>{relatedCost.Department}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Dự án:</td>
                                        <td>{relatedCost.ProjectCode}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Nội dung:</td>
                                        <td>{relatedCost.Content}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Số tiền:</td>
                                        <td style='color: #e74c3c; font-weight: bold;'>{relatedCost.TotalAmount:N0} VNĐ</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold;'>Trạng thái:</td>
                                        <td>{relatedCost.PaymentStatus}</td>
                                    </tr>
                                </table>
                            </div>
                            <p style='margin-top: 20px; font-size: 12px; color: #777;'>Đây là email tự động từ hệ thống QLKH.</p>
                        </div>";
                    }
                }

                await _emailService.SendEmailAsync(user.Email, title, title, emailBody);
            }
            else 
            {
                Console.WriteLine($"User {userId} not found or has no email.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email notification to user {userId}");
            Console.WriteLine($"Error in CostsController.CreateAndSendNotification: {ex.Message}");
        }
    }
}
