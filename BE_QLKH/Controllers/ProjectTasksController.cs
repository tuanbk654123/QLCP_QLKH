using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/project-tasks")]
[Authorize]
public class ProjectTasksController : ControllerBase
{
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<Project> _projects;
    private readonly IMongoCollection<ProjectModule> _modules;
    private readonly IMongoCollection<ProjectTask> _tasks;
    private readonly IMongoCollection<ProjectTaskActivity> _activities;
    private readonly IMongoCollection<User> _users;

    public ProjectTasksController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        _db = client.GetDatabase(options.Value.DatabaseName);
        _projects = _db.GetCollection<Project>("projects");
        _modules = _db.GetCollection<ProjectModule>("project_modules");
        _tasks = _db.GetCollection<ProjectTask>("project_tasks");
        _activities = _db.GetCollection<ProjectTaskActivity>("project_task_activities");
        _users = _db.GetCollection<User>("users");
    }

    private static string GetRole(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private int GetActorLegacyId()
    {
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdStr, out var legacyId) ? legacyId : 0;
    }

    private static bool CanViewAllInCompany(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    private static bool CanAssignTasks(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    private static int GetWeight(ProjectTask task)
    {
        var w = task.EstimatedMinutes ?? 1;
        return Math.Max(1, w);
    }

    private static int ComputeProgress(int weightedSum, int weightSum)
    {
        if (weightSum <= 0) return 0;
        var p = (int)Math.Round(weightedSum * 1.0 / weightSum);
        return Math.Clamp(p, 0, 100);
    }

    private static bool IsOverdue(ProjectTask task, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(task.EndDate)) return false;
        if (task.Status == "DONE" || task.Status == "CANCELLED") return false;
        if (!DateTime.TryParse(task.EndDate, out var end)) return false;
        return end.Date < utcNow.Date;
    }

    private static int SafeAverageProgress(IEnumerable<ProjectTask> tasks)
    {
        var list = tasks.ToList();
        if (list.Count == 0) return 0;
        var avg = (int)Math.Round(list.Average(t => Math.Clamp(t.Progress, 0, 100)));
        return Math.Clamp(avg, 0, 100);
    }

    private async Task LogActivity(string companyId, ProjectTask task, int actorId, string type, string? fromValue, string? toValue, string? message, object? meta)
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var act = new ProjectTaskActivity
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CompanyId = companyId,
            TaskId = task.Id,
            TaskLegacyId = task.LegacyId,
            ActorUserId = actorId > 0 ? actorId : null,
            Type = type,
            FromValue = fromValue,
            ToValue = toValue,
            Message = message,
            MetaJson = meta != null ? JsonSerializer.Serialize(meta) : null,
            CreatedAt = now
        };
        await _activities.InsertOneAsync(act);
    }

    private async Task RecomputeModuleAndProjectProgress(string projectId, string moduleId)
    {
        var module = await _modules.Find(x => x.Id == moduleId).FirstOrDefaultAsync();
        if (module != null)
        {
            var mProgress = ComputeProgress(module.WeightedProgressSum, module.WeightSum);
            await _modules.UpdateOneAsync(x => x.Id == module.Id, Builders<ProjectModule>.Update.Set(x => x.Progress, mProgress));
        }

        var project = await _projects.Find(x => x.Id == projectId).FirstOrDefaultAsync();
        if (project != null)
        {
            var pProgress = ComputeProgress(project.WeightedProgressSum, project.WeightSum);
            await _projects.UpdateOneAsync(x => x.Id == project.Id, Builders<Project>.Update.Set(x => x.Progress, pProgress));
        }
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetTasks(
        [FromQuery] int? projectId,
        [FromQuery] int? moduleId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var builder = Builders<ProjectTask>.Filter;
        var filter = TenantContext.ScopeFilter<ProjectTask>(User);

        if (projectId.HasValue)
        {
            var project = await _projects.Find(TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, projectId.Value)).FirstOrDefaultAsync();
            if (project == null) return NotFound(new { message = "Project not found" });
            filter &= builder.Eq(t => t.ProjectId, project.Id);
        }

        if (moduleId.HasValue)
        {
            var module = await _modules.Find(TenantContext.ScopeFilter<ProjectModule>(User) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, moduleId.Value)).FirstOrDefaultAsync();
            if (module == null) return NotFound(new { message = "Module not found" });
            filter &= builder.Eq(t => t.ModuleId, module.Id);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filter &= builder.Eq(t => t.Status, status);
        }

        if (!CanViewAllInCompany(role))
        {
            filter &= builder.Eq(t => t.AssigneeUserId, actorId);
        }

        var skip = (page - 1) * pageSize;
        var total = await _tasks.CountDocumentsAsync(filter);
        var items = await _tasks.Find(filter).SortByDescending(t => t.LegacyId).Skip(skip).Limit(pageSize).ToListAsync();

        return Ok(new
        {
            items = items.Select(t => new
            {
                id = t.LegacyId,
                name = t.Name,
                projectId = t.ProjectLegacyId,
                moduleId = t.ModuleLegacyId,
                assigneeUserId = t.AssigneeUserId,
                assignerUserId = t.AssignerUserId,
                status = t.Status,
                priority = t.Priority,
                progress = t.Progress,
                description = t.Description,
                notes = t.Notes,
                startDate = t.StartDate,
                endDate = t.EndDate,
                tags = t.Tags,
                estimatedMinutes = t.EstimatedMinutes,
                actualMinutes = t.ActualMinutes,
                deadlineOverdue = t.DeadlineOverdue,
                attachments = t.Attachments,
                createdAt = t.CreatedAt,
                createdBy = t.CreatedBy,
                updatedAt = t.UpdatedAt,
                updatedBy = t.UpdatedBy
            }),
            total
        });
    }

    [HttpGet("kanban")]
    public async Task<ActionResult<object>> GetKanban([FromQuery] int? projectId, [FromQuery] int? moduleId)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var builder = Builders<ProjectTask>.Filter;
        var filter = TenantContext.ScopeFilter<ProjectTask>(User);

        if (projectId.HasValue)
        {
            var project = await _projects.Find(TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, projectId.Value)).FirstOrDefaultAsync();
            if (project == null) return NotFound(new { message = "Project not found" });
            filter &= builder.Eq(t => t.ProjectId, project.Id);
        }

        if (moduleId.HasValue)
        {
            var module = await _modules.Find(TenantContext.ScopeFilter<ProjectModule>(User) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, moduleId.Value)).FirstOrDefaultAsync();
            if (module == null) return NotFound(new { message = "Module not found" });
            filter &= builder.Eq(t => t.ModuleId, module.Id);
        }

        if (!CanViewAllInCompany(role))
        {
            filter &= builder.Eq(t => t.AssigneeUserId, actorId);
        }

        var items = await _tasks.Find(filter).SortByDescending(t => t.LegacyId).ToListAsync();
        var groups = items
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Select(t => new
            {
                id = t.LegacyId,
                name = t.Name,
                projectId = t.ProjectLegacyId,
                moduleId = t.ModuleLegacyId,
                assigneeUserId = t.AssigneeUserId,
                status = t.Status,
                priority = t.Priority,
                progress = t.Progress,
                startDate = t.StartDate,
                endDate = t.EndDate,
                deadlineOverdue = t.DeadlineOverdue
            }).ToList());

        return Ok(new { columns = groups });
    }

    [HttpGet("team-summary")]
    public async Task<ActionResult<object>> GetTeamSummary([FromQuery] int projectId, [FromQuery] int? moduleId)
    {
        var role = GetRole(User);
        if (!CanViewAllInCompany(role)) return StatusCode(403, new { message = "Bạn không có quyền xem thống kê nhân viên" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        var project = await _projects.Find(TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, projectId)).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        string? moduleObjectId = null;
        if (moduleId.HasValue)
        {
            var module = await _modules.Find(TenantContext.ScopeFilter<ProjectModule>(User) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, moduleId.Value)).FirstOrDefaultAsync();
            if (module == null) return NotFound(new { message = "Module not found" });
            if (module.ProjectId != project.Id) return BadRequest(new { message = "Module không thuộc dự án" });
            moduleObjectId = module.Id;
        }

        var builder = Builders<ProjectTask>.Filter;
        var filter = TenantContext.ScopeFilter<ProjectTask>(User) & builder.Eq(t => t.ProjectId, project.Id);
        if (!string.IsNullOrWhiteSpace(moduleObjectId))
        {
            filter &= builder.Eq(t => t.ModuleId, moduleObjectId);
        }

        var tasks = await _tasks.Find(filter).ToListAsync();

        var assigneeIds = tasks
            .Where(t => t.AssigneeUserId.HasValue)
            .Select(t => t.AssigneeUserId!.Value)
            .Distinct()
            .ToList();

        var users = assigneeIds.Count > 0
            ? await _users.Find(u => assigneeIds.Contains(u.LegacyId)).ToListAsync()
            : new List<User>();
        var userMap = users.ToDictionary(u => u.LegacyId, u => u.FullName);

        var nowUtc = DateTime.UtcNow;

        var groups = tasks
            .GroupBy(t => t.AssigneeUserId ?? 0)
            .Select(g =>
            {
                var list = g.ToList();
                var total = list.Count;
                var notStarted = list.Count(x => x.Status == "NOT_STARTED");
                var inProgress = list.Count(x => x.Status == "IN_PROGRESS");
                var inReview = list.Count(x => x.Status == "IN_REVIEW");
                var done = list.Count(x => x.Status == "DONE");
                var paused = list.Count(x => x.Status == "PAUSED");
                var cancelled = list.Count(x => x.Status == "CANCELLED");
                var overdue = list.Count(x => x.DeadlineOverdue || IsOverdue(x, nowUtc));
                var workload = list.Sum(x => Math.Max(1, x.EstimatedMinutes ?? 1));
                var avgProgress = SafeAverageProgress(list);
                var doneRate = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;

                var assigneeId = g.Key;
                var name = assigneeId > 0 && userMap.TryGetValue(assigneeId, out var n) ? n : (assigneeId > 0 ? $"User #{assigneeId}" : "Chưa giao");

                return new
                {
                    assigneeUserId = assigneeId == 0 ? (int?)null : assigneeId,
                    assigneeName = name,
                    total,
                    notStarted,
                    inProgress,
                    inReview,
                    done,
                    paused,
                    cancelled,
                    overdue,
                    avgProgress,
                    workloadMinutes = workload,
                    doneRate
                };
            })
            .OrderByDescending(x => x.overdue)
            .ThenByDescending(x => x.total)
            .ToList();

        return Ok(new
        {
            projectId = project.LegacyId,
            moduleId,
            items = groups,
            totalTasks = tasks.Count,
            overdueTasks = tasks.Count(x => x.DeadlineOverdue || IsOverdue(x, nowUtc))
        });
    }

    [HttpGet("team-tasks")]
    public async Task<ActionResult<object>> GetTeamTasks(
        [FromQuery] int projectId,
        [FromQuery] int? assigneeUserId,
        [FromQuery] int? moduleId,
        [FromQuery] string? status,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var role = GetRole(User);
        if (!CanViewAllInCompany(role)) return StatusCode(403, new { message = "Bạn không có quyền xem task nhân viên" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var project = await _projects.Find(TenantContext.ScopeFilter<Project>(User) & Builders<Project>.Filter.Eq(p => p.LegacyId, projectId)).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        string? moduleObjectId = null;
        if (moduleId.HasValue)
        {
            var module = await _modules.Find(TenantContext.ScopeFilter<ProjectModule>(User) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, moduleId.Value)).FirstOrDefaultAsync();
            if (module == null) return NotFound(new { message = "Module not found" });
            if (module.ProjectId != project.Id) return BadRequest(new { message = "Module không thuộc dự án" });
            moduleObjectId = module.Id;
        }

        var builder = Builders<ProjectTask>.Filter;
        var filter = TenantContext.ScopeFilter<ProjectTask>(User) & builder.Eq(t => t.ProjectId, project.Id);

        if (!string.IsNullOrWhiteSpace(moduleObjectId))
        {
            filter &= builder.Eq(t => t.ModuleId, moduleObjectId);
        }

        if (assigneeUserId.HasValue)
        {
            filter &= builder.Eq(t => t.AssigneeUserId, assigneeUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filter &= builder.Eq(t => t.Status, status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter &= builder.Regex(t => t.Name, new BsonRegularExpression(search, "i"));
        }

        if (overdueOnly)
        {
            filter &= builder.Eq(t => t.DeadlineOverdue, true);
        }

        var skip = (page - 1) * pageSize;
        var total = await _tasks.CountDocumentsAsync(filter);
        var items = await _tasks.Find(filter).SortByDescending(t => t.LegacyId).Skip(skip).Limit(pageSize).ToListAsync();

        var moduleIds = items.Select(x => x.ModuleId).Distinct().ToList();
        var modules = moduleIds.Count > 0 ? await _modules.Find(m => moduleIds.Contains(m.Id)).ToListAsync() : new List<ProjectModule>();
        var moduleMap = modules.ToDictionary(m => m.Id, m => new { m.LegacyId, m.Name });

        var userIds = items.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();
        var users = userIds.Count > 0 ? await _users.Find(u => userIds.Contains(u.LegacyId)).ToListAsync() : new List<User>();
        var userMap = users.ToDictionary(u => u.LegacyId, u => u.FullName);

        return Ok(new
        {
            items = items.Select(t => new
            {
                id = t.LegacyId,
                name = t.Name,
                moduleId = t.ModuleLegacyId,
                moduleName = moduleMap.TryGetValue(t.ModuleId, out var mm) ? mm.Name : string.Empty,
                assigneeUserId = t.AssigneeUserId,
                assigneeName = t.AssigneeUserId.HasValue && userMap.TryGetValue(t.AssigneeUserId.Value, out var un) ? un : string.Empty,
                status = t.Status,
                priority = t.Priority,
                progress = t.Progress,
                startDate = t.StartDate,
                endDate = t.EndDate,
                estimatedMinutes = t.EstimatedMinutes,
                actualMinutes = t.ActualMinutes,
                deadlineOverdue = t.DeadlineOverdue
            }),
            total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetTask(int id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var filter = TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.LegacyId, id);
        var task = await _tasks.Find(filter).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found" });

        if (!CanViewAllInCompany(role))
        {
            if (task.AssigneeUserId != actorId && task.CreatedBy != actorId) return StatusCode(403, new { message = "Bạn không có quyền xem task này" });
        }

        var acts = await _activities.Find(TenantContext.CompanyFilter<ProjectTaskActivity>(companyId) & Builders<ProjectTaskActivity>.Filter.Eq(a => a.TaskId, task.Id))
            .SortByDescending(a => a.CreatedAt)
            .Limit(50)
            .ToListAsync();

        return Ok(new
        {
            id = task.LegacyId,
            name = task.Name,
            projectId = task.ProjectLegacyId,
            moduleId = task.ModuleLegacyId,
            assigneeUserId = task.AssigneeUserId,
            assignerUserId = task.AssignerUserId,
            status = task.Status,
            priority = task.Priority,
            progress = task.Progress,
            description = task.Description,
            notes = task.Notes,
            startDate = task.StartDate,
            endDate = task.EndDate,
            tags = task.Tags,
            estimatedMinutes = task.EstimatedMinutes,
            actualMinutes = task.ActualMinutes,
            deadlineOverdue = task.DeadlineOverdue,
            attachments = task.Attachments,
            createdAt = task.CreatedAt,
            createdBy = task.CreatedBy,
            updatedAt = task.UpdatedAt,
            updatedBy = task.UpdatedBy,
            activities = acts.Select(a => new
            {
                id = a.Id,
                type = a.Type,
                message = a.Message,
                fromValue = a.FromValue,
                toValue = a.ToValue,
                metaJson = a.MetaJson,
                createdAt = a.CreatedAt,
                actorUserId = a.ActorUserId
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var project = await _projects.Find(TenantContext.CompanyFilter<Project>(companyId) & Builders<Project>.Filter.Eq(p => p.LegacyId, request.ProjectId)).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found" });

        var module = await _modules.Find(TenantContext.CompanyFilter<ProjectModule>(companyId) & Builders<ProjectModule>.Filter.Eq(m => m.LegacyId, request.ModuleId)).FirstOrDefaultAsync();
        if (module == null) return NotFound(new { message = "Module not found" });

        if (module.ProjectId != project.Id) return BadRequest(new { message = "Module không thuộc dự án" });

        var assignee = CanAssignTasks(role) ? request.AssigneeUserId : (actorId > 0 ? actorId : null);
        var progress = Math.Clamp(request.Progress ?? 0, 0, 100);
        var status = string.IsNullOrWhiteSpace(request.Status) ? "NOT_STARTED" : request.Status!;
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "MEDIUM" : request.Priority!;

        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Thiếu tên công việc" });

        var task = new ProjectTask
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CompanyId = companyId,
            ProjectId = project.Id,
            ProjectLegacyId = project.LegacyId,
            ModuleId = module.Id,
            ModuleLegacyId = module.LegacyId,
            LegacyId = await TenantContext.GetNextLegacyIdAsync(_db, companyId, "project_tasks", HttpContext.RequestAborted),
            Name = request.Name,
            AssigneeUserId = assignee,
            AssignerUserId = actorId > 0 ? actorId : null,
            Status = status,
            Priority = priority,
            Progress = progress,
            Description = request.Description,
            Notes = request.Notes,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Tags = request.Tags ?? new List<string>(),
            EstimatedMinutes = request.EstimatedMinutes,
            ActualMinutes = request.ActualMinutes,
            Attachments = request.Attachments ?? new List<ProjectTaskAttachment>(),
            CreatedAt = now,
            CreatedBy = actorId,
            UpdatedAt = now,
            UpdatedBy = actorId
        };
        task.DeadlineOverdue = IsOverdue(task, DateTime.UtcNow);

        await _tasks.InsertOneAsync(task);

        var weight = GetWeight(task);
        var deltaWeighted = weight * task.Progress;
        var deltaDone = task.Status == "DONE" ? 1 : 0;

        var moduleUpdate = Builders<ProjectModule>.Update
            .Inc(m => m.TaskCount, 1)
            .Inc(m => m.TaskDoneCount, deltaDone)
            .Inc(m => m.WeightedProgressSum, deltaWeighted)
            .Inc(m => m.WeightSum, weight)
            .Set(m => m.UpdatedAt, now)
            .Set(m => m.UpdatedBy, actorId);
        await _modules.UpdateOneAsync(x => x.Id == module.Id, moduleUpdate);

        var projectUpdate = Builders<Project>.Update
            .Inc(p => p.TaskCount, 1)
            .Inc(p => p.TaskDoneCount, deltaDone)
            .Inc(p => p.WeightedProgressSum, deltaWeighted)
            .Inc(p => p.WeightSum, weight)
            .Set(p => p.UpdatedAt, now)
            .Set(p => p.UpdatedBy, actorId);
        await _projects.UpdateOneAsync(x => x.Id == project.Id, projectUpdate);

        await RecomputeModuleAndProjectProgress(project.Id, module.Id);

        await LogActivity(companyId, task, actorId, "created", null, null, "Tạo task", new { });

        return Ok(new { id = task.LegacyId });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateTask(int id, [FromBody] UpdateTaskRequest input)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var filter = TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.LegacyId, id);
        var task = await _tasks.Find(filter).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found" });

        if (!CanViewAllInCompany(role))
        {
            if (task.AssigneeUserId != actorId && task.CreatedBy != actorId) return StatusCode(403, new { message = "Bạn không có quyền cập nhật task này" });
        }

        var oldStatus = task.Status;
        var oldProgress = task.Progress;
        var oldWeight = GetWeight(task);
        var oldDone = task.Status == "DONE" ? 1 : 0;

        var newStatus = string.IsNullOrWhiteSpace(input.Status) ? task.Status : input.Status!;
        var newProgress = input.Progress.HasValue ? Math.Clamp(input.Progress.Value, 0, 100) : task.Progress;
        var newEstimated = input.EstimatedMinutes.HasValue ? input.EstimatedMinutes : task.EstimatedMinutes;
        var newWeight = Math.Max(1, newEstimated ?? 1);
        var newDone = newStatus == "DONE" ? 1 : 0;

        var newAssignee = task.AssigneeUserId;
        if (CanAssignTasks(role))
        {
            if (input.AssigneeUserId.HasValue) newAssignee = input.AssigneeUserId;
        }

        var assigneeChanged = input.AssigneeUserId.HasValue && input.AssigneeUserId != task.AssigneeUserId;
        var statusChanged = !string.IsNullOrWhiteSpace(input.Status) && input.Status != oldStatus;
        var progressChanged = input.Progress.HasValue && input.Progress.Value != oldProgress;

        if (!string.IsNullOrWhiteSpace(input.Name)) task.Name = input.Name;
        if (input.Description != null) task.Description = input.Description;
        if (input.Notes != null) task.Notes = input.Notes;
        if (input.StartDate != null) task.StartDate = input.StartDate;
        if (input.EndDate != null) task.EndDate = input.EndDate;
        if (!string.IsNullOrWhiteSpace(input.Priority)) task.Priority = input.Priority!;
        task.Status = newStatus;
        task.Progress = newProgress;
        task.AssigneeUserId = newAssignee;
        task.EstimatedMinutes = newEstimated;
        if (input.ActualMinutes.HasValue) task.ActualMinutes = input.ActualMinutes;
        if (input.Tags != null) task.Tags = input.Tags;
        if (input.Attachments != null) task.Attachments = input.Attachments;
        task.DeadlineOverdue = IsOverdue(task, DateTime.UtcNow);
        task.UpdatedAt = now;
        task.UpdatedBy = actorId;

        await _tasks.ReplaceOneAsync(x => x.Id == task.Id, task);

        var deltaTaskDone = newDone - oldDone;
        var deltaWeighted = (newWeight * newProgress) - (oldWeight * oldProgress);
        var deltaWeight = newWeight - oldWeight;

        if (deltaTaskDone != 0 || deltaWeighted != 0 || deltaWeight != 0)
        {
            var moduleUpdate = Builders<ProjectModule>.Update
                .Inc(m => m.TaskDoneCount, deltaTaskDone)
                .Inc(m => m.WeightedProgressSum, deltaWeighted)
                .Inc(m => m.WeightSum, deltaWeight)
                .Set(m => m.UpdatedAt, now)
                .Set(m => m.UpdatedBy, actorId);
            await _modules.UpdateOneAsync(x => x.Id == task.ModuleId, moduleUpdate);

            var projectUpdate = Builders<Project>.Update
                .Inc(p => p.TaskDoneCount, deltaTaskDone)
                .Inc(p => p.WeightedProgressSum, deltaWeighted)
                .Inc(p => p.WeightSum, deltaWeight)
                .Set(p => p.UpdatedAt, now)
                .Set(p => p.UpdatedBy, actorId);
            await _projects.UpdateOneAsync(x => x.Id == task.ProjectId, projectUpdate);

            await RecomputeModuleAndProjectProgress(task.ProjectId, task.ModuleId);
        }

        if (statusChanged)
        {
            await LogActivity(companyId, task, actorId, "status_changed", oldStatus, task.Status, null, null);
        }
        if (progressChanged)
        {
            await LogActivity(companyId, task, actorId, "progress_changed", oldProgress.ToString(), task.Progress.ToString(), null, null);
        }
        if (assigneeChanged)
        {
            await LogActivity(companyId, task, actorId, "assigned", null, task.AssigneeUserId?.ToString(), null, null);
        }

        return Ok(new { message = "Task updated" });
    }

    [HttpPost("{id:int}/comment")]
    public async Task<ActionResult<object>> AddComment(int id, [FromBody] CommentRequest request)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();

        var task = await _tasks.Find(TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.LegacyId, id)).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found" });

        if (!CanViewAllInCompany(role))
        {
            if (task.AssigneeUserId != actorId && task.CreatedBy != actorId) return StatusCode(403, new { message = "Bạn không có quyền bình luận task này" });
        }

        await LogActivity(companyId, task, actorId, "comment", null, null, request.Message, null);
        return Ok(new { message = "Comment added" });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteTask(int id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var role = GetRole(User);
        var actorId = GetActorLegacyId();
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var filter = TenantContext.CompanyFilter<ProjectTask>(companyId) & Builders<ProjectTask>.Filter.Eq(t => t.LegacyId, id);
        var task = await _tasks.Find(filter).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found" });

        if (!CanViewAllInCompany(role))
        {
            if (task.AssigneeUserId != actorId && task.CreatedBy != actorId) return StatusCode(403, new { message = "Bạn không có quyền xóa task này" });
        }

        await _tasks.DeleteOneAsync(x => x.Id == task.Id);
        await _activities.DeleteManyAsync(TenantContext.CompanyFilter<ProjectTaskActivity>(companyId) & Builders<ProjectTaskActivity>.Filter.Eq(a => a.TaskId, task.Id));

        var weight = GetWeight(task);
        var deltaWeighted = -(weight * task.Progress);
        var deltaWeight = -weight;
        var deltaDone = task.Status == "DONE" ? -1 : 0;

        var moduleUpdate = Builders<ProjectModule>.Update
            .Inc(m => m.TaskCount, -1)
            .Inc(m => m.TaskDoneCount, deltaDone)
            .Inc(m => m.WeightedProgressSum, deltaWeighted)
            .Inc(m => m.WeightSum, deltaWeight)
            .Set(m => m.UpdatedAt, now)
            .Set(m => m.UpdatedBy, actorId);
        await _modules.UpdateOneAsync(x => x.Id == task.ModuleId, moduleUpdate);

        var projectUpdate = Builders<Project>.Update
            .Inc(p => p.TaskCount, -1)
            .Inc(p => p.TaskDoneCount, deltaDone)
            .Inc(p => p.WeightedProgressSum, deltaWeighted)
            .Inc(p => p.WeightSum, deltaWeight)
            .Set(p => p.UpdatedAt, now)
            .Set(p => p.UpdatedBy, actorId);
        await _projects.UpdateOneAsync(x => x.Id == task.ProjectId, projectUpdate);

        await RecomputeModuleAndProjectProgress(task.ProjectId, task.ModuleId);

        return Ok(new { message = "Task deleted" });
    }

    public class CommentRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class CreateTaskRequest
    {
        public int ProjectId { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? AssigneeUserId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? Progress { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string>? Tags { get; set; }
        public int? EstimatedMinutes { get; set; }
        public int? ActualMinutes { get; set; }
        public List<ProjectTaskAttachment>? Attachments { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string? Name { get; set; }
        public int? AssigneeUserId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? Progress { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string>? Tags { get; set; }
        public int? EstimatedMinutes { get; set; }
        public int? ActualMinutes { get; set; }
        public List<ProjectTaskAttachment>? Attachments { get; set; }
    }
}
