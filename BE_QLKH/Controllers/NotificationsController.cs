using BE_QLKH.Hubs;
using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMongoCollection<Notification> _notifications;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<UserCompany> _userCompanies;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IEmailService _emailService;

    public NotificationsController(
        IMongoClient client, 
        IOptions<MongoDbSettings> options, 
        IHubContext<NotificationsHub> hubContext,
        IEmailService emailService)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _notifications = db.GetCollection<Notification>("notifications");
        _users = db.GetCollection<User>("users");
        _userCompanies = db.GetCollection<UserCompany>("user_companies");
        _hubContext = hubContext;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetNotifications([FromQuery] int page = 1)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId) &
                     TenantContext.CompanyFilter<Notification>(companyId);
        var total = await _notifications.CountDocumentsAsync(filter);
        
        var notifications = await _notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * 20)
            .Limit(20)
            .ToListAsync();

        var unreadCount = await _notifications.CountDocumentsAsync(
            Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.IsRead, false),
                TenantContext.CompanyFilter<Notification>(companyId)
            )
        );

        return Ok(new
        {
            notifications,
            total,
            unreadCount
        });
    }

    [HttpPost("mark-read/{id}")]
    public async Task<ActionResult<object>> MarkAsRead(string id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
         var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Id, id),
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            TenantContext.CompanyFilter<Notification>(companyId)
        );
        
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        
        var result = await _notifications.UpdateOneAsync(filter, update);
        
        if (result.MatchedCount == 0) return NotFound();
        
        return Ok(new { message = "Marked as read" });
    }
    
    [HttpPost("mark-all-read")]
    public async Task<ActionResult<object>> MarkAllAsRead()
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
         var userIdStr = User.FindFirst("legacy_id")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId) &
                     TenantContext.CompanyFilter<Notification>(companyId);
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        
        await _notifications.UpdateManyAsync(filter, update);
        
        return Ok(new { message = "Marked all as read" });
    }

    [HttpPost("create")]
    public async Task<ActionResult<object>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var createdCount = 0;
        foreach (var userId in request.UserIds)
        {
             var ok = await _userCompanies.Find(x => x.UserLegacyId == userId && x.CompanyId == companyId).AnyAsync();
             if (!ok) continue;
             var notif = new Notification
             {
                 Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                 UserId = userId,
                 CompanyId = companyId,
                 Title = request.Title,
                 Message = request.Message,
                 Type = request.Type,
                 RelatedId = request.RelatedId,
                 IsRead = false,
                 CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
             };
             
             await _notifications.InsertOneAsync(notif);
             createdCount++;
             
             // Real-time
             try 
             {
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notif);
             }
             catch
             {
                 // Ignore SignalR errors
             }
        }
        
        return Ok(new { message = "Notifications created", count = createdCount });
    }
}

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RelatedId { get; set; } = string.Empty;
    public List<int> UserIds { get; set; } = new();
}
