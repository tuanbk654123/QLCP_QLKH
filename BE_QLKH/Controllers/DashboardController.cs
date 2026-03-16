using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Security.Claims;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMongoCollection<Customer> _customers;
    private readonly IMongoCollection<Cost> _costs;
    private readonly IMongoCollection<UserCompany> _userCompanies;

    public DashboardController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _customers = db.GetCollection<Customer>("customers");
        _costs = db.GetCollection<Cost>("costs");
        _userCompanies = db.GetCollection<UserCompany>("user_companies");
    }

    private static IEnumerable<Customer> FilterCustomersByDate(IEnumerable<Customer> customers, string? fromDate, string? toDate)
    {
        DateTime? from = null;
        DateTime? to = null;

        if (!string.IsNullOrWhiteSpace(fromDate) &&
            DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromParsed))
        {
            from = fromParsed.Date;
        }

        if (!string.IsNullOrWhiteSpace(toDate) &&
            DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toParsed))
        {
            to = toParsed.Date;
        }

        if (!from.HasValue && !to.HasValue)
        {
            return customers;
        }

        return customers.Where(c =>
        {
            if (string.IsNullOrWhiteSpace(c.JoinDate))
            {
                return false;
            }

            if (!DateTime.TryParseExact(c.JoinDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return false;
            }

            if (from.HasValue && date.Date < from.Value)
            {
                return false;
            }

            if (to.HasValue && date.Date > to.Value)
            {
                return false;
            }

            return true;
        });
    }

    private static IEnumerable<Cost> FilterCostsByDate(IEnumerable<Cost> costs, string? fromDate, string? toDate)
    {
        DateTime? from = null;
        DateTime? to = null;

        if (!string.IsNullOrWhiteSpace(fromDate) &&
            DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromParsed))
        {
            from = fromParsed.Date;
        }

        if (!string.IsNullOrWhiteSpace(toDate) &&
            DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toParsed))
        {
            to = toParsed.Date;
        }

        if (!from.HasValue && !to.HasValue)
        {
            return costs;
        }

        return costs.Where(c =>
        {
            var dateStr = string.IsNullOrWhiteSpace(c.VoucherDate) ? c.RequestDate : c.VoucherDate;
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return false;
            }

            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return false;
            }

            if (from.HasValue && date.Date < from.Value)
            {
                return false;
            }

            if (to.HasValue && date.Date > to.Value)
            {
                return false;
            }

            return true;
        });
    }

    [HttpGet("overview")]
    public async Task<ActionResult<object>> GetOverview([FromQuery] string? fromDate, [FromQuery] string? toDate)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var allCustomers = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId)).ToListAsync();
        var filteredCustomers = FilterCustomersByDate(allCustomers, fromDate, toDate);
        var totalCustomers = filteredCustomers.Count();
        var activeCustomers = filteredCustomers.Count(c => c.Status == "active");

        var allCosts = await _costs.Find(TenantContext.CompanyFilter<Cost>(companyId)).ToListAsync();
        var filteredCosts = FilterCostsByDate(allCosts, fromDate, toDate);

        var revenueCosts = filteredCosts.Where(c => c.TransactionType == "Thu" && c.PaymentStatus == "Đã thanh toán");
        var totalRevenue = revenueCosts.Sum(c => c.TotalAmount);

        var expenseCosts = filteredCosts.Where(c => c.TransactionType == "Chi" && c.PaymentStatus == "Đã thanh toán");
        var totalExpense = expenseCosts.Sum(c => c.TotalAmount);

        return Ok(new
        {
            totalCustomers,
            activeCustomers,
            totalRevenue,
            totalExpense
        });
    }

    [HttpGet("overview-all")]
    public async Task<ActionResult<object>> GetOverviewAll([FromQuery] string? fromDate, [FromQuery] string? toDate)
    {
        if (!CanViewAllCompaniesDashboard()) return StatusCode(403, new { message = "Bạn không có quyền xem Dashboard tổng" });

        var companyIds = await GetAccessibleCompanyIds();
        if (companyIds.Count == 0) return Ok(new { totalCustomers = 0, activeCustomers = 0, totalRevenue = 0m, totalExpense = 0m });

        var customerFilter = BuildCompanyIdsFilter<Customer>(companyIds);
        var allCustomers = await _customers.Find(customerFilter).ToListAsync();
        var filteredCustomers = FilterCustomersByDate(allCustomers, fromDate, toDate);
        var totalCustomers = filteredCustomers.Count();
        var activeCustomers = filteredCustomers.Count(c => c.Status == "active");

        var costsFilter = BuildCompanyIdsFilter<Cost>(companyIds);
        var allCosts = await _costs.Find(costsFilter).ToListAsync();
        var filteredCosts = FilterCostsByDate(allCosts, fromDate, toDate);

        var revenueCosts = filteredCosts.Where(c => c.TransactionType == "Thu" && c.PaymentStatus == "Đã thanh toán");
        var totalRevenue = revenueCosts.Sum(c => c.TotalAmount);

        var expenseCosts = filteredCosts.Where(c => c.TransactionType == "Chi" && c.PaymentStatus == "Đã thanh toán");
        var totalExpense = expenseCosts.Sum(c => c.TotalAmount);

        return Ok(new
        {
            totalCustomers,
            activeCustomers,
            totalRevenue,
            totalExpense
        });
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<object>> GetTransactions([FromQuery] string? fromDate, [FromQuery] string? toDate)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var allCosts = await _costs.Find(TenantContext.CompanyFilter<Cost>(companyId)).ToListAsync();
        var filteredCosts = FilterCostsByDate(allCosts, fromDate, toDate);

        var transactions = filteredCosts.Select(c => new
        {
            date = string.IsNullOrWhiteSpace(c.VoucherDate) ? c.RequestDate : c.VoucherDate,
            type = c.TransactionType == "Thu" ? "revenue" : "expense",
            amount = c.TotalAmount,
            status = c.PaymentStatus == "Đã thanh toán" ? "completed" : "pending",
            description = c.Content,
            transactionType = c.TransactionType,
            paymentStatus = c.PaymentStatus
        });

        return Ok(new
        {
            transactions
        });
    }

    [HttpGet("transactions-all")]
    public async Task<ActionResult<object>> GetTransactionsAll([FromQuery] string? fromDate, [FromQuery] string? toDate)
    {
        if (!CanViewAllCompaniesDashboard()) return StatusCode(403, new { message = "Bạn không có quyền xem Dashboard tổng" });
        var companyIds = await GetAccessibleCompanyIds();
        if (companyIds.Count == 0) return Ok(new { transactions = Array.Empty<object>() });

        var costsFilter = BuildCompanyIdsFilter<Cost>(companyIds);
        var allCosts = await _costs.Find(costsFilter).ToListAsync();
        var filteredCosts = FilterCostsByDate(allCosts, fromDate, toDate);

        var transactions = filteredCosts.Select(c => new
        {
            date = string.IsNullOrWhiteSpace(c.VoucherDate) ? c.RequestDate : c.VoucherDate,
            type = c.TransactionType == "Thu" ? "revenue" : "expense",
            amount = c.TotalAmount,
            status = c.PaymentStatus == "Đã thanh toán" ? "completed" : "pending",
            description = c.Content,
            transactionType = c.TransactionType,
            paymentStatus = c.PaymentStatus
        });

        return Ok(new { transactions });
    }

    [HttpGet("customer-growth")]
    public async Task<ActionResult<object>> GetCustomerGrowth([FromQuery] int year)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var allCustomers = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId)).ToListAsync();
        
        // Filter by year and group by month
        var monthlyStats = allCustomers
            .Where(c => !string.IsNullOrWhiteSpace(c.JoinDate) && 
                        DateTime.TryParseExact(c.JoinDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                        date.Year == year)
            .GroupBy(c => 
            {
                DateTime.TryParseExact(c.JoinDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
                return date.Month;
            })
            .Select(g => new 
            {
                Month = g.Key,
                Count = g.Count(),
                ConsultedCount = g.Count(c => !string.IsNullOrEmpty(c.ConsultingStatus) && c.ConsultingStatus.ToLower().Contains("tư vấn"))
            })
            .OrderBy(x => x.Month)
            .ToList();

        // Fill missing months with 0
        var result = Enumerable.Range(1, 12).Select(month => 
        {
            var stat = monthlyStats.FirstOrDefault(s => s.Month == month);
            return new 
            {
                name = $"Tháng {month}",
                Total = stat?.Count ?? 0,
                Consulted = stat?.ConsultedCount ?? 0
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("customer-growth-all")]
    public async Task<ActionResult<object>> GetCustomerGrowthAll([FromQuery] int year)
    {
        if (!CanViewAllCompaniesDashboard()) return StatusCode(403, new { message = "Bạn không có quyền xem Dashboard tổng" });
        var companyIds = await GetAccessibleCompanyIds();
        if (companyIds.Count == 0) return Ok(Enumerable.Range(1, 12).Select(m => new { name = $"Tháng {m}", Total = 0, Consulted = 0 }).ToList());

        var customerFilter = BuildCompanyIdsFilter<Customer>(companyIds);
        var allCustomers = await _customers.Find(customerFilter).ToListAsync();

        var monthlyStats = allCustomers
            .Where(c => !string.IsNullOrWhiteSpace(c.JoinDate) &&
                        DateTime.TryParseExact(c.JoinDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                        date.Year == year)
            .GroupBy(c =>
            {
                DateTime.TryParseExact(c.JoinDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
                return date.Month;
            })
            .Select(g => new
            {
                Month = g.Key,
                Count = g.Count(),
                ConsultedCount = g.Count(c => !string.IsNullOrEmpty(c.ConsultingStatus) && c.ConsultingStatus.ToLower().Contains("tư vấn"))
            })
            .OrderBy(x => x.Month)
            .ToList();

        var result = Enumerable.Range(1, 12).Select(month =>
        {
            var stat = monthlyStats.FirstOrDefault(s => s.Month == month);
            return new
            {
                name = $"Tháng {month}",
                Total = stat?.Count ?? 0,
                Consulted = stat?.ConsultedCount ?? 0
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("project-costs")]
    public async Task<ActionResult<object>> GetProjectCosts([FromQuery] int? month, [FromQuery] int year)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var allCosts = await _costs.Find(TenantContext.CompanyFilter<Cost>(companyId)).ToListAsync();

        // Filter by PaymentStatus "Đã thanh toán" and Date
        var query = allCosts.Where(c => c.PaymentStatus == "Đã thanh toán");

        if (year > 0)
        {
            query = query.Where(c => 
            {
                var dateStr = string.IsNullOrWhiteSpace(c.VoucherDate) ? c.RequestDate : c.VoucherDate;
                if (string.IsNullOrWhiteSpace(dateStr)) return false;
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return false;
                
                if (month.HasValue && date.Month != month.Value) return false;
                if (date.Year != year) return false;

                return true;
            });
        }

        var projectStats = query
            .GroupBy(c => string.IsNullOrEmpty(c.ProjectCode) ? "Khác" : c.ProjectCode)
            .Select(g => new 
            {
                name = g.Key,
                value = g.Sum(c => c.TotalAmount)
            })
            .OrderByDescending(x => x.value)
            .ToList();

        return Ok(projectStats);
    }

    [HttpGet("project-costs-all")]
    public async Task<ActionResult<object>> GetProjectCostsAll([FromQuery] int? month, [FromQuery] int year)
    {
        if (!CanViewAllCompaniesDashboard()) return StatusCode(403, new { message = "Bạn không có quyền xem Dashboard tổng" });
        var companyIds = await GetAccessibleCompanyIds();
        if (companyIds.Count == 0) return Ok(new List<object>());

        var costsFilter = BuildCompanyIdsFilter<Cost>(companyIds);
        var allCosts = await _costs.Find(costsFilter).ToListAsync();
        var query = allCosts.Where(c => c.PaymentStatus == "Đã thanh toán");

        if (year > 0)
        {
            query = query.Where(c =>
            {
                var dateStr = string.IsNullOrWhiteSpace(c.VoucherDate) ? c.RequestDate : c.VoucherDate;
                if (string.IsNullOrWhiteSpace(dateStr)) return false;
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return false;

                if (month.HasValue && date.Month != month.Value) return false;
                if (date.Year != year) return false;

                return true;
            });
        }

        var projectStats = query
            .GroupBy(c => string.IsNullOrEmpty(c.ProjectCode) ? "Khác" : c.ProjectCode)
            .Select(g => new
            {
                name = g.Key,
                value = g.Sum(c => c.TotalAmount)
            })
            .OrderByDescending(x => x.value)
            .ToList();

        return Ok(projectStats);
    }

    private bool CanViewAllCompaniesDashboard()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "assistant_ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "hr", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<string>> GetAccessibleCompanyIds()
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        if (legacyIdClaim == null || !int.TryParse(legacyIdClaim, out var legacyId)) return new List<string>();
        var ids = await _userCompanies.Find(x => x.UserLegacyId == legacyId).Project(x => x.CompanyId).ToListAsync();
        return ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
    }

    private static FilterDefinition<T> BuildCompanyIdsFilter<T>(List<string> companyIds)
    {
        var strIds = companyIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var objIds = strIds.Select(x => ObjectId.TryParse(x, out var oid) ? (ObjectId?)oid : null).Where(x => x.HasValue).Select(x => x!.Value).ToList();

        if (objIds.Count > 0)
        {
            return Builders<T>.Filter.Or(
                Builders<T>.Filter.In("company_id", objIds),
                Builders<T>.Filter.In("company_id", strIds)
            );
        }

        return Builders<T>.Filter.In("company_id", strIds);
    }
}
