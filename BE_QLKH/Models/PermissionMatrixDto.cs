namespace BE_QLKH.Models;

public class PermissionMatrixDto
{
    public List<RoleDto> Roles { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> QlkhPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> QlcpPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> UserPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> DashboardPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> ExportPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> SchedulingPermissions { get; set; } = new();
    public List<PermissionFieldGroupDto> QlkhFields { get; set; } = new();
    public List<PermissionFieldGroupDto> QlcpFields { get; set; } = new();
    public List<PermissionFieldGroupDto> UserFields { get; set; } = new();
    public List<PermissionFieldGroupDto> DashboardFields { get; set; } = new();
    public List<PermissionFieldGroupDto> ExportFields { get; set; } = new();
    public List<PermissionFieldGroupDto> SchedulingFields { get; set; } = new();
}

public class RoleDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class PermissionFieldGroupDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<PermissionFieldDto> Children { get; set; } = new();
}

public class PermissionFieldDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
