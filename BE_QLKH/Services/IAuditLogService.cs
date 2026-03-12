using BE_QLKH.Models;

namespace BE_QLKH.Services;

public interface IAuditLogService
{
    Task LogAsync(string entityType, int entityLegacyId, string action, User? actor, object? oldData, object? newData);
}

