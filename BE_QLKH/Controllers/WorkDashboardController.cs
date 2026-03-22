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
[Route("api/work-dashboard")]
[Authorize]
public class WorkDashboardController : ControllerBase
{
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<ProjectTask> _tasks;
    private readonly IMongoCollection<Project> _projects;
    private readonly IMongoCollection<ProjectModule> _modules;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Company> _companies;

    public WorkDashboardController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        _db = client.GetDatabase(options.Value.DatabaseName);
        _tasks = _db.GetCollection<ProjectTask>("project_tasks");
        _projects = _db.GetCollection<Project>("projects");
        _modules = _db.GetCollection<ProjectModule>("project_modules");
        _users = _db.GetCollection<User>("users");
        _companies = _db.GetCollection<Company>("companies");
    }

    private static string GetRole(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private int GetActorLegacyId()
    {
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdStr, out var legacyId) ? legacyId : 0;
    }

    private static bool CanViewCompanyDashboard(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly" };
        return roles.Contains(role);
    }

    private static bool CanViewAllCompanies(string role)
    {
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "assistant_ceo", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(List<string> CompanyIds, bool SelfOnly, int SelfLegacyId)> ResolveScopeAsync(string role, DashboardFilterRequest req)
    {
        var selfLegacyId = GetActorLegacyId();

        if (CanViewAllCompanies(role))
        {
            var all = await _companies.Find(c => c.Status == "active").Project(c => c.Id).ToListAsync();
            var ids = all.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(req.CompanyId))
            {
                if (!ids.Contains(req.CompanyId)) return (new List<string>(), false, selfLegacyId);
                return (new List<string> { req.CompanyId }, false, selfLegacyId);
            }
            return (ids, false, selfLegacyId);
        }

        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        if (CanViewCompanyDashboard(role))
        {
            return (new List<string> { companyId }, false, selfLegacyId);
        }

        if (selfLegacyId <= 0)
        {
            return (new List<string>(), true, selfLegacyId);
        }

        req.CompanyId = null;
        req.AssigneeUserId = selfLegacyId;
        return (new List<string> { companyId }, true, selfLegacyId);
    }

    private FilterDefinition<ProjectTask> BuildTaskFilter(List<string> companyIds, DashboardFilterRequest req)
    {
        var builder = Builders<ProjectTask>.Filter;
        var filter = TenantContext.CompanyIdsFilter<ProjectTask>(companyIds);

        if (req.AssigneeUserId.HasValue)
        {
            filter &= builder.Eq(t => t.AssigneeUserId, req.AssigneeUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            filter &= builder.Eq(t => t.Status, req.Status);
        }

        if (req.OverdueOnly == true)
        {
            filter &= builder.Eq(t => t.DeadlineOverdue, true);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            filter &= builder.Regex(t => t.Name, new BsonRegularExpression(req.Search, "i"));
        }

        if (!string.IsNullOrWhiteSpace(req.FromDate))
        {
            filter &= builder.Gte("end_date", req.FromDate);
        }

        if (!string.IsNullOrWhiteSpace(req.ToDate))
        {
            filter &= builder.Lte("end_date", req.ToDate);
        }

        return filter;
    }

    private static string? ParseSortDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        if (DateTime.TryParse(dateStr, out var dt)) return dt.ToString("yyyy-MM-dd");
        return dateStr;
    }

    [HttpGet("companies")]
    public async Task<ActionResult<object>> GetCompanies()
    {
        var role = GetRole(User);
        if (!CanViewAllCompanies(role)) return StatusCode(403, new { message = "Bạn không có quyền xem danh sách công ty" });

        var (ids, _, _) = await ResolveScopeAsync(role, new DashboardFilterRequest());
        var companies = await _companies.Find(c => ids.Contains(c.Id) && c.Status == "active").ToListAsync();
        return Ok(new
        {
            items = companies.Select(c => new { id = c.Id, code = c.Code, name = c.Name }).OrderBy(x => x.code).ToList()
        });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] DashboardFilterRequest req)
    {
        var role = GetRole(User);
        var (companyIds, _, _) = await ResolveScopeAsync(role, req);
        if (companyIds.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });

        var filter = BuildTaskFilter(companyIds, req);
        var tasks = await _tasks.Find(filter).Project(t => new { t.Status, t.Progress, t.DeadlineOverdue }).ToListAsync();

        var total = tasks.Count;
        var overdue = tasks.Count(x => x.DeadlineOverdue);
        var notStarted = tasks.Count(x => x.Status == "NOT_STARTED");
        var inProgress = tasks.Count(x => x.Status == "IN_PROGRESS");
        var inReview = tasks.Count(x => x.Status == "IN_REVIEW");
        var done = tasks.Count(x => x.Status == "DONE");
        var paused = tasks.Count(x => x.Status == "PAUSED");
        var cancelled = tasks.Count(x => x.Status == "CANCELLED");
        var avgProgress = total > 0 ? (int)Math.Round(tasks.Average(x => Math.Clamp(x.Progress, 0, 100))) : 0;
        var doneRate = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;

        return Ok(new
        {
            total,
            overdue,
            avgProgress,
            doneRate,
            byStatus = new
            {
                notStarted,
                inProgress,
                inReview,
                done,
                paused,
                cancelled
            }
        });
    }

    [HttpGet("team")]
    public async Task<ActionResult<object>> GetTeam([FromQuery] DashboardFilterRequest req)
    {
        var role = GetRole(User);
        var (companyIds, _, _) = await ResolveScopeAsync(role, req);
        if (companyIds.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });

        var filter = BuildTaskFilter(companyIds, req);
        var tasks = await _tasks.Find(filter).Project(t => new
        {
            t.AssigneeUserId,
            t.Status,
            t.Progress,
            t.DeadlineOverdue,
            t.EstimatedMinutes
        }).ToListAsync();

        var assigneeIds = tasks.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();
        var users = assigneeIds.Count > 0 ? await _users.Find(u => assigneeIds.Contains(u.LegacyId)).ToListAsync() : new List<User>();
        var userMap = users.ToDictionary(u => u.LegacyId, u => u.FullName);

        var items = tasks
            .GroupBy(t => t.AssigneeUserId ?? 0)
            .Select(g =>
            {
                var list = g.ToList();
                var total = list.Count;
                var done = list.Count(x => x.Status == "DONE");
                var overdue = list.Count(x => x.DeadlineOverdue);
                var avgProgress = total > 0 ? (int)Math.Round(list.Average(x => Math.Clamp(x.Progress, 0, 100))) : 0;
                var doneRate = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;
                var workload = list.Sum(x => Math.Max(1, x.EstimatedMinutes ?? 1));
                var assigneeId = g.Key;
                var name = assigneeId > 0 && userMap.TryGetValue(assigneeId, out var n) ? n : (assigneeId > 0 ? $"User #{assigneeId}" : "Chưa giao");

                return new
                {
                    assigneeUserId = assigneeId == 0 ? (int?)null : assigneeId,
                    assigneeName = name,
                    total,
                    overdue,
                    avgProgress,
                    doneRate,
                    workloadMinutes = workload
                };
            })
            .OrderByDescending(x => x.overdue)
            .ThenByDescending(x => x.total)
            .ToList();

        return Ok(new { items });
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<object>> GetTasks([FromQuery] DashboardFilterRequest req, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var role = GetRole(User);
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var (companyIds, _, _) = await ResolveScopeAsync(role, req);
        if (companyIds.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });

        var filter = BuildTaskFilter(companyIds, req);
        var skip = (page - 1) * pageSize;

        var total = await _tasks.CountDocumentsAsync(filter);
        var items = await _tasks.Find(filter).SortByDescending(t => t.LegacyId).Skip(skip).Limit(pageSize).ToListAsync();

        var moduleIds = items.Select(x => x.ModuleId).Distinct().ToList();
        var modules = moduleIds.Count > 0 ? await _modules.Find(m => moduleIds.Contains(m.Id)).ToListAsync() : new List<ProjectModule>();
        var moduleMap = modules.ToDictionary(m => m.Id, m => m.Name);

        var projectIds = items.Select(x => x.ProjectId).Distinct().ToList();
        var projects = projectIds.Count > 0 ? await _projects.Find(p => projectIds.Contains(p.Id)).ToListAsync() : new List<Project>();
        var projectMap = projects.ToDictionary(p => p.Id, p => new { p.Code, p.Name });

        var assigneeIds = items.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();
        var users = assigneeIds.Count > 0 ? await _users.Find(u => assigneeIds.Contains(u.LegacyId)).ToListAsync() : new List<User>();
        var userMap = users.ToDictionary(u => u.LegacyId, u => u.FullName);

        var companies = companyIds.Count > 0 ? await _companies.Find(c => companyIds.Contains(c.Id)).ToListAsync() : new List<Company>();
        var companyMap = companies.ToDictionary(c => c.Id, c => new { c.Code, c.Name });

        return Ok(new
        {
            total,
            items = items.Select(t => new
            {
                id = t.LegacyId,
                name = t.Name,
                companyId = t.CompanyId,
                companyCode = companyMap.TryGetValue(t.CompanyId, out var cm) ? cm.Code : string.Empty,
                projectCode = projectMap.TryGetValue(t.ProjectId, out var pm) ? pm.Code : string.Empty,
                projectName = projectMap.TryGetValue(t.ProjectId, out var pm2) ? pm2.Name : string.Empty,
                moduleName = moduleMap.TryGetValue(t.ModuleId, out var mn) ? mn : string.Empty,
                assigneeUserId = t.AssigneeUserId,
                assigneeName = t.AssigneeUserId.HasValue && userMap.TryGetValue(t.AssigneeUserId.Value, out var un) ? un : string.Empty,
                status = t.Status,
                priority = t.Priority,
                progress = t.Progress,
                startDate = t.StartDate,
                endDate = t.EndDate,
                deadlineOverdue = t.DeadlineOverdue,
                estimatedMinutes = t.EstimatedMinutes,
                updatedAt = t.UpdatedAt
            })
        });
    }

    [HttpGet("user-kanban")]
    public async Task<ActionResult<object>> GetUserKanban([FromQuery] DashboardFilterRequest req)
    {
        var role = GetRole(User);
        if (!req.AssigneeUserId.HasValue) return BadRequest(new { message = "Thiếu assigneeUserId" });

        var (companyIds, selfOnly, selfLegacyId) = await ResolveScopeAsync(role, req);
        if (companyIds.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });
        if (selfOnly && req.AssigneeUserId != selfLegacyId) return StatusCode(403, new { message = "Bạn không có quyền xem dữ liệu người khác" });

        var filter = BuildTaskFilter(companyIds, req);
        var items = await _tasks.Find(filter).SortByDescending(t => t.LegacyId).Limit(500).ToListAsync();

        var moduleIds = items.Select(x => x.ModuleId).Distinct().ToList();
        var modules = moduleIds.Count > 0 ? await _modules.Find(m => moduleIds.Contains(m.Id)).ToListAsync() : new List<ProjectModule>();
        var moduleMap = modules.ToDictionary(m => m.Id, m => m.Name);

        var projectIds = items.Select(x => x.ProjectId).Distinct().ToList();
        var projects = projectIds.Count > 0 ? await _projects.Find(p => projectIds.Contains(p.Id)).ToListAsync() : new List<Project>();
        var projectMap = projects.ToDictionary(p => p.Id, p => new { p.Code, p.Name });

        var columns = items
            .GroupBy(t => t.Status ?? "NOT_STARTED")
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new
                {
                    id = t.LegacyId,
                    name = t.Name,
                    companyId = t.CompanyId,
                    projectCode = projectMap.TryGetValue(t.ProjectId, out var pm) ? pm.Code : string.Empty,
                    projectName = projectMap.TryGetValue(t.ProjectId, out var pm2) ? pm2.Name : string.Empty,
                    moduleName = moduleMap.TryGetValue(t.ModuleId, out var mn) ? mn : string.Empty,
                    priority = t.Priority,
                    progress = t.Progress,
                    startDate = t.StartDate,
                    endDate = t.EndDate,
                    deadlineOverdue = t.DeadlineOverdue
                }).ToList()
            );

        return Ok(new { columns });
    }

    [HttpGet("user-timeline")]
    public async Task<ActionResult<object>> GetUserTimeline([FromQuery] DashboardFilterRequest req, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var role = GetRole(User);
        if (!req.AssigneeUserId.HasValue) return BadRequest(new { message = "Thiếu assigneeUserId" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var (companyIds, selfOnly, selfLegacyId) = await ResolveScopeAsync(role, req);
        if (companyIds.Count == 0) return StatusCode(403, new { message = "Bạn không có quyền truy cập công ty này" });
        if (selfOnly && req.AssigneeUserId != selfLegacyId) return StatusCode(403, new { message = "Bạn không có quyền xem dữ liệu người khác" });

        var filter = BuildTaskFilter(companyIds, req);
        var itemsAll = await _tasks.Find(filter).ToListAsync();

        var itemsSorted = itemsAll
            .OrderBy(t => ParseSortDate(t.EndDate) ?? "9999-12-31")
            .ThenByDescending(t => t.LegacyId)
            .ToList();

        var total = itemsSorted.Count;
        var skip = (page - 1) * pageSize;
        var items = itemsSorted.Skip(skip).Take(pageSize).ToList();

        var moduleIds = items.Select(x => x.ModuleId).Distinct().ToList();
        var modules = moduleIds.Count > 0 ? await _modules.Find(m => moduleIds.Contains(m.Id)).ToListAsync() : new List<ProjectModule>();
        var moduleMap = modules.ToDictionary(m => m.Id, m => m.Name);

        var projectIds = items.Select(x => x.ProjectId).Distinct().ToList();
        var projects = projectIds.Count > 0 ? await _projects.Find(p => projectIds.Contains(p.Id)).ToListAsync() : new List<Project>();
        var projectMap = projects.ToDictionary(p => p.Id, p => new { p.Code, p.Name });

        return Ok(new
        {
            total,
            items = items.Select(t => new
            {
                id = t.LegacyId,
                name = t.Name,
                projectCode = projectMap.TryGetValue(t.ProjectId, out var pm) ? pm.Code : string.Empty,
                projectName = projectMap.TryGetValue(t.ProjectId, out var pm2) ? pm2.Name : string.Empty,
                moduleName = moduleMap.TryGetValue(t.ModuleId, out var mn) ? mn : string.Empty,
                status = t.Status,
                priority = t.Priority,
                progress = t.Progress,
                startDate = t.StartDate,
                endDate = t.EndDate,
                deadlineOverdue = t.DeadlineOverdue,
                estimatedMinutes = t.EstimatedMinutes,
                updatedAt = t.UpdatedAt
            })
        });
    }

    public class DashboardFilterRequest
    {
        public string? CompanyId { get; set; }
        public int? AssigneeUserId { get; set; }
        public string? Status { get; set; }
        public bool? OverdueOnly { get; set; }
        public string? Search { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }
}
