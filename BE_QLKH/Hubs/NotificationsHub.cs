using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BE_QLKH.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var legacyId = Context.User?.FindFirst("legacy_id")?.Value;
        _logger.LogInformation($"SignalR Connected: {Context.ConnectionId}, LegacyId: {legacyId}");

        if (legacyId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, legacyId);
            _logger.LogInformation($"Added connection {Context.ConnectionId} to group {legacyId}");
        }
        else 
        {
            _logger.LogWarning($"Connection {Context.ConnectionId} has no legacy_id claim");
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var legacyId = Context.User?.FindFirst("legacy_id")?.Value;
        if (legacyId != null)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, legacyId);
        }
        return base.OnDisconnectedAsync(exception);
    }
}
