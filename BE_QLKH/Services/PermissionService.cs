using BE_QLKH.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BE_QLKH.Services;

public class PermissionService : IPermissionService
{
    private readonly IMongoCollection<Role> _roles;
    private readonly IMongoCollection<FieldDef> _fields;
    private readonly IMongoCollection<FieldPermission> _fieldPermissions;

    public PermissionService(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _roles = db.GetCollection<Role>("roles");
        _fields = db.GetCollection<FieldDef>("fields");
        _fieldPermissions = db.GetCollection<FieldPermission>("field_permissions");
    }

    public async Task<PermissionMatrixDto> GetPermissionMatrixAsync()
    {
        var roles = await _roles.Find(r => r.IsActive).ToListAsync();
        var fields = await _fields.Find(_ => true).ToListAsync();
        var fieldPermissions = await _fieldPermissions.Find(_ => true).ToListAsync();

        var roleDtos = roles.Select(r => new RoleDto
        {
            Key = r.Code,
            Label = r.Name
        }).ToList();

        var qlkhFields = fields.Where(f => f.ModuleCode == "qlkh").ToList();
        var qlcpFields = fields.Where(f => f.ModuleCode == "qlcp").ToList();
        var userFields = fields.Where(f => f.ModuleCode == "users").ToList();
        var dashboardFields = fields.Where(f => f.ModuleCode == "dashboard").ToList();
        var exportFields = fields.Where(f => f.ModuleCode == "export").ToList();
        var schedulingFields = fields.Where(f => f.ModuleCode == "scheduling").ToList();

        var qlkhGroups = BuildFieldGroups(qlkhFields);
        var qlcpGroups = BuildFieldGroups(qlcpFields);
        var userGroups = BuildFieldGroups(userFields);
        var dashboardGroups = BuildFieldGroups(dashboardFields);
        var exportGroups = BuildFieldGroups(exportFields);
        var schedulingGroups = BuildFieldGroups(schedulingFields);

        var qlkhPerm = BuildPermissionMap("qlkh", qlkhFields, roles, fieldPermissions);
        var qlcpPerm = BuildPermissionMap("qlcp", qlcpFields, roles, fieldPermissions);
        var userPerm = BuildPermissionMap("users", userFields, roles, fieldPermissions);
        var dashboardPerm = BuildPermissionMap("dashboard", dashboardFields, roles, fieldPermissions);
        var exportPerm = BuildPermissionMap("export", exportFields, roles, fieldPermissions);
        var schedulingPerm = BuildPermissionMap("scheduling", schedulingFields, roles, fieldPermissions);

        return new PermissionMatrixDto
        {
            Roles = roleDtos,
            QlkhFields = qlkhGroups,
            QlcpFields = qlcpGroups,
            UserFields = userGroups,
            DashboardFields = dashboardGroups,
            ExportFields = exportGroups,
            SchedulingFields = schedulingGroups,
            QlkhPermissions = qlkhPerm,
            QlcpPermissions = qlcpPerm,
            UserPermissions = userPerm,
            DashboardPermissions = dashboardPerm,
            ExportPermissions = exportPerm,
            SchedulingPermissions = schedulingPerm
        };
    }

    public async Task SavePermissionMatrixAsync(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> permissions)
    {
        var writeModels = new List<WriteModel<FieldPermission>>();

        foreach (var moduleEntry in permissions)
        {
            var moduleCode = moduleEntry.Key;
            foreach (var fieldEntry in moduleEntry.Value)
            {
                var fieldCode = fieldEntry.Key;
                foreach (var roleEntry in fieldEntry.Value)
                {
                    var roleCode = roleEntry.Key;
                    var level = roleEntry.Value;

                    var filter = Builders<FieldPermission>.Filter.Where(p =>
                        p.ModuleCode == moduleCode &&
                        p.FieldCode == fieldCode &&
                        p.RoleCode == roleCode);

                    var update = Builders<FieldPermission>.Update
                        .Set(p => p.PermissionLevel, level)
                        .SetOnInsert(p => p.ModuleCode, moduleCode)
                        .SetOnInsert(p => p.FieldCode, fieldCode)
                        .SetOnInsert(p => p.RoleCode, roleCode);

                    writeModels.Add(new UpdateOneModel<FieldPermission>(filter, update) { IsUpsert = true });
                }
            }
        }

        if (writeModels.Any())
        {
            await _fieldPermissions.BulkWriteAsync(writeModels);
        }
    }

    public async Task<Dictionary<string, string>> GetRolePermissionsForModuleAsync(string moduleCode, string roleCode)
    {
        var fields = await _fields.Find(f => f.ModuleCode == moduleCode).ToListAsync();
        var fieldCodes = fields.Select(f => f.Code).ToList();

        var filter = Builders<FieldPermission>.Filter.Where(p =>
            p.ModuleCode == moduleCode &&
            fieldCodes.Contains(p.FieldCode) &&
            p.RoleCode == roleCode);

        var permissions = await _fieldPermissions.Find(filter).ToListAsync();

        var result = new Dictionary<string, string>();

        foreach (var field in fields)
        {
            var fp = permissions.FirstOrDefault(p => p.FieldCode == field.Code);
            var level = fp?.PermissionLevel ?? "R";
            result[field.Code] = level;
        }

        return result;
    }

    public async Task EnsureFieldAsync(string moduleCode, string code, string label, string groupCode, string groupLabel)
    {
        var exists = await _fields.Find(f => f.ModuleCode == moduleCode && f.Code == code).AnyAsync();
        if (!exists)
        {
            var field = new FieldDef
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                ModuleCode = moduleCode,
                Code = code,
                Label = label,
                GroupCode = groupCode,
                GroupLabel = groupLabel,
                OrderIndex = 999 // Put at the end
            };
            await _fields.InsertOneAsync(field);

            // Add default permissions for Admin/CEO
            var roles = await _roles.Find(_ => true).ToListAsync();
            foreach (var role in roles)
            {
                var level = (role.Code == "admin" || role.Code == "ceo") ? "A" : "R";
                await _fieldPermissions.InsertOneAsync(new FieldPermission
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    ModuleCode = moduleCode,
                    FieldCode = code,
                    RoleCode = role.Code,
                    PermissionLevel = level
                });
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> BuildPermissionMap(
        string moduleCode,
        List<FieldDef> fields,
        List<Role> roles,
        List<FieldPermission> fieldPermissions)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        foreach (var field in fields)
        {
            var fieldCode = field.Code;
            if (!result.ContainsKey(fieldCode))
            {
                result[fieldCode] = new Dictionary<string, string>();
            }

            foreach (var role in roles)
            {
                var fp = fieldPermissions.FirstOrDefault(p =>
                    p.ModuleCode == moduleCode &&
                    p.FieldCode == fieldCode &&
                    p.RoleCode == role.Code);

                var level = fp?.PermissionLevel ?? "R";
                result[fieldCode][role.Code] = level;
            }
        }

        return result;
    }

    private static List<PermissionFieldGroupDto> BuildFieldGroups(List<FieldDef> fields)
    {
        return fields
            .GroupBy(f => new { f.GroupCode, f.GroupLabel })
            .OrderBy(g => g.Key.GroupLabel)
            .Select(g => new PermissionFieldGroupDto
            {
                Key = g.Key.GroupCode,
                Label = g.Key.GroupLabel,
                Children = g
                    .OrderBy(f => f.OrderIndex)
                    .Select(f => new PermissionFieldDto
                    {
                        Key = f.Code,
                        Label = f.Label
                    }).ToList()
            })
            .ToList();
    }
}
