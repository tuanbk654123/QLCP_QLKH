using BE_QLKH.Models;

namespace BE_QLKH.Services;

public interface IPermissionService
{
    Task<PermissionMatrixDto> GetPermissionMatrixAsync();
    Task SavePermissionMatrixAsync(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> permissions);
    Task<Dictionary<string, string>> GetRolePermissionsForModuleAsync(string moduleCode, string roleCode);
    Task EnsureFieldAsync(string moduleCode, string code, string label, string groupCode, string groupLabel);
}
