using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<Project> _projects;
    private readonly IMongoCollection<ProjectModule> _modules;
    private readonly IMongoCollection<ProjectTask> _tasks;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Customer> _customers;

    public ProjectsController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        _db = client.GetDatabase(options.Value.DatabaseName);
        _projects = _db.GetCollection<Project>("projects");
        _modules = _db.GetCollection<ProjectModule>("project_modules");
        _tasks = _db.GetCollection<ProjectTask>("project_tasks");
        _users = _db.GetCollection<User>("users");
        _customers = _db.GetCollection<Customer>("customers");
    }

    private static string GetRole(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private int GetActorLegacyId()
    {
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdStr, out var legacyId) ? legacyId : 0;
    }

    private static bool CanManageProjects(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    private static bool CanViewAllInCompany(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetProjects([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var builder = Builders<Project>.Filter;
        var filter = TenantContext.ScopeFilter<Project>(User);

        if (!CanViewAllInCompany(role))
        {
            var taskFilter = TenantContext.ScopeFilter<ProjectTask>(User) & Builders<ProjectTask>.Filter.Eq(t => t.AssigneeUserId, actorId);
            var projectIds = await _tasks.Distinct<string>("project_id", taskFilter).ToListAsync();
            if (projectIds.Count == 0)
            {
                return Ok(new { items = Array.Empty<object>(), total = 0 });
            }
            filter &= builder.In(p => p.Id, projectIds);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowered = search.ToLower();
            filter &= builder.Or(
                builder.Regex(p => p.Name, new BsonRegularExpression(lowered, "i")),
                builder.Regex(p => p.Code, new BsonRegularExpression(lowered, "i")),
                builder.Regex(p => p.CustomerName, new BsonRegularExpression(lowered, "i"))
            );
        }

        var total = await _projects.CountDocumentsAsync(filter);
        var skip = (page - 1) * pageSize;
        var items = await _projects.Find(filter).SortByDescending(p => p.LegacyId).Skip(skip).Limit(pageSize).ToListAsync();

        var userIds = items.Where(x => x.ManagerUserId.HasValue).Select(x => x.ManagerUserId!.Value).Distinct().ToList();
        var users = userIds.Count > 0 ? await _users.Find(u => userIds.Contains(u.LegacyId)).ToListAsync() : new List<User>();
        var userMap = users.ToDictionary(u => u.LegacyId, u => u.FullName);

        return Ok(new
        {
            items = items.Select(p => new
            {
                id = p.LegacyId,
                name = p.Name,
                code = p.Code,
                customerLegacyId = p.CustomerLegacyId,
                customerName = p.CustomerName,
                startDate = p.StartDate,
                endDate = p.EndDate,
                managerUserId = p.ManagerUserId,
                managerName = p.ManagerUserId.HasValue && userMap.TryGetValue(p.ManagerUserId.Value, out var mn) ? mn : string.Empty,
                status = p.Status,
                progress = p.Progress,
                budget = p.Budget,
                description = p.Description,
                moduleCount = p.ModuleCount,
                taskCount = p.TaskCount,
                taskDoneCount = p.TaskDoneCount,
                createdAt = p.CreatedAt,
                createdBy = p.CreatedBy,
                updatedAt = p.UpdatedAt,
                updatedBy = p.UpdatedBy
            }),
            total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetProject(int id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var filter = TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, id);
        var project = await _projects.Find(filter).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        if (!CanViewAllInCompany(role))
        {
            var taskFilter = TenantContext.ScopeFilter<ProjectTask>(User) &
                             Builders<ProjectTask>.Filter.Eq(t => t.ProjectId, project.Id) &
                             Builders<ProjectTask>.Filter.Eq(t => t.AssigneeUserId, actorId);
            var hasAny = await _tasks.Find(taskFilter).AnyAsync();
            if (!hasAny) return StatusCode(403, new { message = "Bạn không có quyền xem dự án này" });
        }

        var manager = project.ManagerUserId.HasValue ? await _users.Find(u => u.LegacyId == project.ManagerUserId.Value).FirstOrDefaultAsync() : null;
        return Ok(new
        {
            id = project.LegacyId,
            name = project.Name,
            code = project.Code,
            customerLegacyId = project.CustomerLegacyId,
            customerName = project.CustomerName,
            startDate = project.StartDate,
            endDate = project.EndDate,
            managerUserId = project.ManagerUserId,
            managerName = manager?.FullName ?? string.Empty,
            status = project.Status,
            progress = project.Progress,
            budget = project.Budget,
            description = project.Description,
            moduleCount = project.ModuleCount,
            taskCount = project.TaskCount,
            taskDoneCount = project.TaskDoneCount,
            createdAt = project.CreatedAt,
            createdBy = project.CreatedBy,
            updatedAt = project.UpdatedAt,
            updatedBy = project.UpdatedBy
        });
    }

    [HttpGet("lookups/customers")]
    public async Task<ActionResult<object>> GetCustomerLookups([FromQuery] string? search)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var builder = Builders<Customer>.Filter;
        var filter = TenantContext.CompanyFilter<Customer>(companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            filter &= builder.Or(
                builder.Regex(c => c.Name, new BsonRegularExpression(search, "i")),
                builder.Regex(c => c.Phone, new BsonRegularExpression(search, "i")),
                builder.Regex(c => c.TaxCode, new BsonRegularExpression(search, "i"))
            );
        }
        var items = await _customers.Find(filter).SortByDescending(c => c.LegacyId).Limit(20).ToListAsync();
        return Ok(new
        {
            items = items.Select(c => new { id = c.LegacyId, name = c.Name ?? string.Empty }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateProject([FromBody] Project input)
    {
        var role = GetRole(User);
        if (!CanManageProjects(role)) return StatusCode(403, new { message = "Bạn không có quyền tạo dự án" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        input.Id = ObjectId.GenerateNewId().ToString();
        input.CompanyId = companyId;
        input.LegacyId = await TenantContext.GetNextLegacyIdAsync(_db, companyId, "projects", HttpContext.RequestAborted);
        input.CreatedAt = now;
        input.CreatedBy = actorId;
        input.UpdatedAt = now;
        input.UpdatedBy = actorId;

        if (input.CustomerLegacyId.HasValue)
        {
            var customer = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, input.CustomerLegacyId.Value)).FirstOrDefaultAsync();
            input.CustomerName = customer?.Name;
        }

        var exists = await _projects.Find(TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.Code, input.Code)).AnyAsync();
        if (exists) return BadRequest(new { message = "Mã dự án đã tồn tại" });

        input.Progress = Math.Clamp(input.Progress, 0, 100);
        input.ModuleCount = 0;
        input.TaskCount = 0;
        input.TaskDoneCount = 0;
        input.WeightedProgressSum = 0;
        input.WeightSum = 0;

        await _projects.InsertOneAsync(input);

        return Ok(new { id = input.LegacyId });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateProject(int id, [FromBody] Project input)
    {
        var role = GetRole(User);
        if (!CanManageProjects(role)) return StatusCode(403, new { message = "Bạn không có quyền cập nhật dự án" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var filter = TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.LegacyId, id);
        var project = await _projects.Find(filter).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        if (!string.IsNullOrWhiteSpace(input.Code) && !string.Equals(input.Code, project.Code, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _projects.Find(TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.Code, input.Code)).AnyAsync();
            if (exists) return BadRequest(new { message = "Mã dự án đã tồn tại" });
            project.Code = input.Code;
        }

        project.Name = string.IsNullOrWhiteSpace(input.Name) ? project.Name : input.Name;
        project.StartDate = input.StartDate ?? project.StartDate;
        project.EndDate = input.EndDate ?? project.EndDate;
        project.ManagerUserId = input.ManagerUserId ?? project.ManagerUserId;
        project.Status = string.IsNullOrWhiteSpace(input.Status) ? project.Status : input.Status;
        project.Description = input.Description ?? project.Description;
        project.Budget = input.Budget ?? project.Budget;

        if (input.CustomerLegacyId.HasValue && input.CustomerLegacyId != project.CustomerLegacyId)
        {
            var customer = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, input.CustomerLegacyId.Value)).FirstOrDefaultAsync();
            project.CustomerLegacyId = input.CustomerLegacyId;
            project.CustomerName = customer?.Name;
        }

        project.UpdatedAt = now;
        project.UpdatedBy = actorId;

        await _projects.ReplaceOneAsync(x => x.Id == project.Id, project);
        return Ok(new { message = "Project updated" });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteProject(int id)
    {
        var role = GetRole(User);
        if (!CanManageProjects(role)) return StatusCode(403, new { message = "Bạn không có quyền xóa dự án" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var projectFilter = TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.LegacyId, id);
        var project = await _projects.Find(projectFilter).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        var moduleFilter = TenantContext.CompanyFilter<ProjectModule>(companyId) & Builders<ProjectModule>.Filter.Eq(m => m.ProjectId, project.Id);
        var taskFilter = TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.ProjectId, project.Id);

        await _tasks.DeleteManyAsync(taskFilter);
        await _modules.DeleteManyAsync(moduleFilter);
        await _projects.DeleteOneAsync(projectFilter);

        return Ok(new { message = "Project deleted" });
    }
}
