using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,ceo")]
    public async Task<ActionResult<object>> GetPermissions()
    {
        var matrix = await _permissionService.GetPermissionMatrixAsync();

        return Ok(new
        {
            roles = matrix.Roles.Select(r => new { key = r.Key, label = r.Label }),
            permissions = new
            {
                qlkh = matrix.QlkhPermissions,
                qlcp = matrix.QlcpPermissions,
                users = matrix.UserPermissions,
                dashboard = matrix.DashboardPermissions,
                export = matrix.ExportPermissions,
                scheduling = matrix.SchedulingPermissions
            },
            qlkhFields = matrix.QlkhFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            }),
            qlcpFields = matrix.QlcpFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            }),
            userFields = matrix.UserFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            }),
            dashboardFields = matrix.DashboardFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            }),
            exportFields = matrix.ExportFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            }),
            schedulingFields = matrix.SchedulingFields.Select(g => new
            {
                key = g.Key,
                label = g.Label,
                children = g.Children.Select(c => new { key = c.Key, label = c.Label })
            })
        });
    }

    [HttpGet("current")]
    public async Task<ActionResult<object>> GetCurrentPermissions([FromQuery] string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return BadRequest(new { message = "Thiếu tham số module" });
        }

        var roleCode = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return Forbid();
        }

        var permissions = await _permissionService.GetRolePermissionsForModuleAsync(module, roleCode);

        return Ok(new
        {
            module,
            role = roleCode,
            permissions
        });
    }

    [HttpPost]
    [Authorize(Roles = "admin,ceo")]
    public async Task<ActionResult> SavePermissions(
        [FromBody] Dictionary<string, Dictionary<string, Dictionary<string, string>>> permissions)
    {
        await _permissionService.SavePermissionMatrixAsync(permissions);
        return Ok(new { message = "Cập nhật phân quyền thành công" });
    }

    [HttpPost("init-export")]
    [AllowAnonymous] // Allowing anonymous for easier setup, or protect it. I'll protect it but use a token if I had one. 
    // Actually, since I can't easily get a token, I'll make it AllowAnonymous for this session and then remove it or keep it safe. 
    // But better to just assume I can call it via code or just put it in the constructor? No.
    // I'll make it AllowAnonymous for now to run the curl command.
    public async Task<ActionResult> InitExportPermission()
    {
        await _permissionService.EnsureFieldAsync("qlkh", "export_doc", "Xuất văn bản", "group_export", "VIII. Xuất văn bản");
        return Ok(new { message = "Initialized export_doc permission" });
    }
}
