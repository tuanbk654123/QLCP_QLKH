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
[Route("api/project-modules")]
[Authorize]
public class ProjectModulesController : ControllerBase
{
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<Project> _projects;
    private readonly IMongoCollection<ProjectModule> _modules;
    private readonly IMongoCollection<ProjectTask> _tasks;

    public ProjectModulesController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        _db = client.GetDatabase(options.Value.DatabaseName);
        _projects = _db.GetCollection<Project>("projects");
        _modules = _db.GetCollection<ProjectModule>("project_modules");
        _tasks = _db.GetCollection<ProjectTask>("project_tasks");
    }

    private static string GetRole(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private int GetActorLegacyId()
    {
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdStr, out var legacyId) ? legacyId : 0;
    }

    private static bool CanManageModules(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    private static int ComputeProgress(int weightedSum, int weightSum)
    {
        if (weightSum <= 0) return 0;
        var p = (int)Math.Round(weightedSum * 1.0 / weightSum);
        return Math.Clamp(p, 0, 100);
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetModules([FromQuery] int projectId)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        var project = await _projects.Find(TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, projectId)).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        var filter = TenantContext.ScopeFilter<ProjectModule>(User) & Builders<ProjectModule>.Filter.Eq(m => m.ProjectId, project.Id);
        var items = await _modules.Find(filter).SortBy(m => m.LegacyId).ToListAsync();

        return Ok(new
        {
            items = items.Select(m => new
            {
                id = m.LegacyId,
                name = m.Name,
                ownerUserId = m.OwnerUserId,
                startDate = m.StartDate,
                endDate = m.EndDate,
                status = m.Status,
                progress = m.Progress,
                priority = m.Priority,
                description = m.Description,
                taskCount = m.TaskCount,
                taskDoneCount = m.TaskDoneCount,
                createdAt = m.CreatedAt,
                createdBy = m.CreatedBy,
                updatedAt = m.UpdatedAt,
                updatedBy = m.UpdatedBy
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateModule([FromBody] ProjectModule input)
    {
        var role = GetRole(User);
        if (!CanManageModules(role)) return StatusCode(403, new { message = "Bạn không có quyền tạo module" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var project = await _projects.Find(TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.LegacyId, input.ProjectLegacyId)).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        input.Id = ObjectId.GenerateNewId().ToString();
        input.CompanyId = companyId;
        input.ProjectId = project.Id;
        input.ProjectLegacyId = project.LegacyId;
        input.LegacyId = await TenantContext.GetNextLegacyIdAsync(_db, companyId, "project_modules", HttpContext.RequestAborted);
        input.CreatedAt = now;
        input.CreatedBy = actorId;
        input.UpdatedAt = now;
        input.UpdatedBy = actorId;
        input.Progress = Math.Clamp(input.Progress, 0, 100);
        input.TaskCount = 0;
        input.TaskDoneCount = 0;
        input.WeightedProgressSum = 0;
        input.WeightSum = 0;

        await _modules.InsertOneAsync(input);

        var projectUpdate = Builders<Project>.Update
            .Inc(p => p.ModuleCount, 1)
            .Set(p => p.UpdatedAt, now)
            .Set(p => p.UpdatedBy, actorId);
        await _projects.UpdateOneAsync(x => x.Id == project.Id, projectUpdate);

        return Ok(new { id = input.LegacyId });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateModule(int id, [FromBody] ProjectModule input)
    {
        var role = GetRole(User);
        if (!CanManageModules(role)) return StatusCode(403, new { message = "Bạn không có quyền cập nhật module" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var filter = TenantContext.CompanyFilter<ProjectModule>(companyId) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, id);
        var module = await _modules.Find(filter).FirstOrDefaultAsync();
        if (module == null) return NotFound(new { message = "Module not found" });

        module.Name = string.IsNullOrWhiteSpace(input.Name) ? module.Name : input.Name;
        module.OwnerUserId = input.OwnerUserId ?? module.OwnerUserId;
        module.StartDate = input.StartDate ?? module.StartDate;
        module.EndDate = input.EndDate ?? module.EndDate;
        module.Status = string.IsNullOrWhiteSpace(input.Status) ? module.Status : input.Status;
        module.Priority = string.IsNullOrWhiteSpace(input.Priority) ? module.Priority : input.Priority;
        module.Description = input.Description ?? module.Description;
        module.UpdatedAt = now;
        module.UpdatedBy = actorId;

        await _modules.ReplaceOneAsync(x => x.Id == module.Id, module);

        var project = await _projects.Find(x => x.Id == module.ProjectId).FirstOrDefaultAsync();
        if (project != null)
        {
            var projectUpdate = Builders<Project>.Update
                .Set(p => p.UpdatedAt, now)
                .Set(p => p.UpdatedBy, actorId);
            await _projects.UpdateOneAsync(x => x.Id == project.Id, projectUpdate);
        }

        return Ok(new { message = "Module updated" });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteModule(int id)
    {
        var role = GetRole(User);
        if (!CanManageModules(role)) return StatusCode(403, new { message = "Bạn không có quyền xóa module" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var filter = TenantContext.CompanyFilter<ProjectModule>(companyId) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, id);
        var module = await _modules.Find(filter).FirstOrDefaultAsync();
        if (module == null) return NotFound(new { message = "Module not found" });

        var taskFilter = TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.ModuleId, module.Id);
        var tasks = await _tasks.Find(taskFilter).ToListAsync();

        var deltaTaskCount = -tasks.Count;
        var deltaDone = -tasks.Count(t => t.Status == "DONE");
        var deltaWeighted = -tasks.Sum(t => Math.Max(1, t.EstimatedMinutes ?? 1) * Math.Clamp(t.Progress, 0, 100));
        var deltaWeight = -tasks.Sum(t => Math.Max(1, t.EstimatedMinutes ?? 1));

        await _tasks.DeleteManyAsync(taskFilter);
        await _modules.DeleteOneAsync(x => x.Id == module.Id);

        var project = await _projects.Find(x => x.Id == module.ProjectId).FirstOrDefaultAsync();
        if (project != null)
        {
            var projUpdate = Builders<Project>.Update
                .Inc(p => p.ModuleCount, -1)
                .Inc(p => p.TaskCount, deltaTaskCount)
                .Inc(p => p.TaskDoneCount, deltaDone)
                .Inc(p => p.WeightedProgressSum, deltaWeighted)
                .Inc(p => p.WeightSum, deltaWeight)
                .Set(p => p.UpdatedAt, now)
                .Set(p => p.UpdatedBy, actorId);

            await _projects.UpdateOneAsync(x => x.Id == project.Id, projUpdate);

            var refreshed = await _projects.Find(x => x.Id == project.Id).FirstOrDefaultAsync();
            if (refreshed != null)
            {
                var progress = ComputeProgress(refreshed.WeightedProgressSum, refreshed.WeightSum);
                await _projects.UpdateOneAsync(x => x.Id == refreshed.Id, Builders<Project>.Update.Set(p => p.Progress, progress));
            }
        }

        return Ok(new { message = "Module deleted" });
    }
}
