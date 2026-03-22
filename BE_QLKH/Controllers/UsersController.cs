using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Company> _companies;
    private readonly IMongoCollection<UserCompany> _userCompanies;
    private readonly IMongoCollection<Role> _roles;

    public UsersController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _users = db.GetCollection<User>("users");
        _companies = db.GetCollection<Company>("companies");
        _userCompanies = db.GetCollection<UserCompany>("user_companies");
        _roles = db.GetCollection<Role>("roles");
    }

    private static string GetRole(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    private int GetActorLegacyId()
    {
        var legacyIdStr = User.FindFirst("legacy_id")?.Value;
        return int.TryParse(legacyIdStr, out var legacyId) ? legacyId : 0;
    }

    private static bool CanViewAllCompanies(string role)
    {
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "assistant_ceo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanViewCompanyUsers(string role)
    {
        var roles = new[] { "admin", "ceo", "assistant_ceo", "director", "giam_doc", "assistant_director", "ip_manager", "manager", "quan_ly", "hr" };
        return roles.Contains(role);
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetUsers(
        [FromQuery] string? search, 
        [FromQuery] int page = 1,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        if (page < 1) page = 1;
        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var role = GetRole(User);
        var actorLegacyId = GetActorLegacyId();
        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        var builder = Builders<User>.Filter;
        var filter = builder.Empty;

        if (CanViewAllCompanies(role))
        {
        }
        else if (CanViewCompanyUsers(role))
        {
            filter &= builder.Eq(u => u.CompanyId, companyId);
        }
        else
        {
            if (actorLegacyId <= 0) return StatusCode(403, new { message = "Bạn không có quyền" });
            filter &= builder.Eq(u => u.LegacyId, actorLegacyId);
        }

        // Global Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search), "i");
            filter &= builder.Or(
                builder.Regex(u => u.FullName, searchRegex),
                builder.Regex(u => u.Email, searchRegex),
                builder.Regex(u => u.Username, searchRegex),
                builder.Regex(u => u.EmployeeCode, searchRegex),
                builder.Regex(u => u.Phone, searchRegex),
                builder.Regex(u => u.Department, searchRegex),
                builder.Regex(u => u.Position, searchRegex)
            );
        }

        // Column Filters via Reflection
        var properties = typeof(User).GetProperties();
        foreach (var query in Request.Query)
        {
            var key = query.Key;
            var value = query.Value.ToString();
            
            if (string.IsNullOrEmpty(value)) continue;
            if (new[] { "search", "page", "sortfield", "sortorder", "limit" }.Contains(key.ToLower())) continue;

            // Handle special mapping for role
            var propName = key;
            if (key.Equals("role", StringComparison.OrdinalIgnoreCase)) propName = "RoleCode";

            var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                var bsonAttr = prop.GetCustomAttributes(typeof(BsonElementAttribute), false)
                    .FirstOrDefault() as BsonElementAttribute;
                var dbField = bsonAttr?.ElementName ?? prop.Name;
                
                if (prop.PropertyType == typeof(string))
                {
                    filter &= builder.Regex(dbField, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(value), "i"));
                }
                else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                {
                    if (int.TryParse(value, out int intVal))
                    {
                        filter &= builder.Eq(dbField, intVal);
                    }
                }
                else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                {
                     if (decimal.TryParse(value, out decimal decVal))
                    {
                        filter &= builder.Eq(dbField, decVal);
                    }
                }
                else 
                {
                     filter &= builder.Regex(dbField, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(value), "i"));
                }
            }
        }

        // Sorting
        SortDefinition<User> sort = Builders<User>.Sort.Descending(u => u.LegacyId);
        if (!string.IsNullOrEmpty(sortField))
        {
            var propName = sortField;
            if (sortField.Equals("role", StringComparison.OrdinalIgnoreCase)) propName = "RoleCode";

            var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                var bsonAttr = prop.GetCustomAttributes(typeof(BsonElementAttribute), false)
                    .FirstOrDefault() as BsonElementAttribute;
                var dbField = bsonAttr?.ElementName ?? prop.Name;
                
                if (sortOrder?.ToLower() == "asc" || sortOrder?.ToLower() == "ascend")
                    sort = Builders<User>.Sort.Ascending(dbField);
                else
                    sort = Builders<User>.Sort.Descending(dbField);
            }
        }

        var total = await _users.CountDocumentsAsync(filter);
        var users = await _users
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var result = users.Select(u => new
        {
            id = u.LegacyId,
            userId = u.UserId,
            employeeCode = u.EmployeeCode,
            employmentType = u.EmploymentType,
            username = u.Username,
            email = u.Email,
            fullName = u.FullName,
            phone = u.Phone,
            address = u.Address,
            dob = u.Dob,
            idNumber = u.IdNumber,
            idIssuedDate = u.IdIssuedDate,
            idIssuedPlace = u.IdIssuedPlace,
            personalTaxCode = u.PersonalTaxCode,
            bankName = u.BankName,
            bankAccount = u.BankAccount,
            socialInsuranceNumber = u.SocialInsuranceNumber,
            healthInsuranceNumber = u.HealthInsuranceNumber,
            role = u.RoleCode,
            company = u.Company,
            companyId = u.CompanyId,
            department = u.Department,
            position = u.Position,
            status = u.Status,
            avatar = u.Avatar,
            joinDate = u.JoinDate,
            salary = u.Salary,
            contractType = u.ContractType,
            contractStartDate = u.ContractStartDate,
            contractEndDate = u.ContractEndDate,
            workLocation = u.WorkLocation,
            managerName = u.ManagerName,
            managerId = u.ManagerId,
            emergencyContactName = u.EmergencyContactName,
            emergencyContactPhone = u.EmergencyContactPhone,
            group = u.Group,
            dataScope = u.DataScope,
            directPermission = u.DirectPermission,
            approveRight = u.ApproveRight,
            financeViewRight = u.FinanceViewRight,
            exportDataRight = u.ExportDataRight,
            createdAt = u.CreatedAt,
            createdBy = u.CreatedBy,
            updatedAt = u.UpdatedAt,
            updatedBy = u.UpdatedBy,
            offboardDate = u.OffboardDate,
            notes = u.Notes
        });

        return Ok(new
        {
            users = result,
            userCount = total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetUserByLegacyId(int id)
    {
        var role = GetRole(User);
        var actorLegacyId = GetActorLegacyId();
        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        if (!CanViewAllCompanies(role) && !CanViewCompanyUsers(role))
        {
            if (actorLegacyId <= 0 || actorLegacyId != id) return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        if (!CanViewAllCompanies(role) && CanViewCompanyUsers(role) && user.CompanyId != companyId)
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        return Ok(new
        {
            id = user.LegacyId,
            userId = user.UserId,
            employeeCode = user.EmployeeCode,
            employmentType = user.EmploymentType,
            username = user.Username,
            email = user.Email,
            fullName = user.FullName,
            phone = user.Phone,
            address = user.Address,
            dob = user.Dob,
            idNumber = user.IdNumber,
            idIssuedDate = user.IdIssuedDate,
            idIssuedPlace = user.IdIssuedPlace,
            personalTaxCode = user.PersonalTaxCode,
            bankName = user.BankName,
            bankAccount = user.BankAccount,
            socialInsuranceNumber = user.SocialInsuranceNumber,
            healthInsuranceNumber = user.HealthInsuranceNumber,
            role = user.RoleCode,
            company = user.Company,
            companyId = user.CompanyId,
            department = user.Department,
            position = user.Position,
            status = user.Status,
            avatar = user.Avatar,
            joinDate = user.JoinDate,
            salary = user.Salary,
            contractType = user.ContractType,
            contractStartDate = user.ContractStartDate,
            contractEndDate = user.ContractEndDate,
            workLocation = user.WorkLocation,
            managerName = user.ManagerName,
            managerId = user.ManagerId,
            emergencyContactName = user.EmergencyContactName,
            emergencyContactPhone = user.EmergencyContactPhone,
            group = user.Group,
            dataScope = user.DataScope,
            directPermission = user.DirectPermission,
            approveRight = user.ApproveRight,
            financeViewRight = user.FinanceViewRight,
            exportDataRight = user.ExportDataRight,
            createdAt = user.CreatedAt,
            createdBy = user.CreatedBy,
            updatedAt = user.UpdatedAt,
            updatedBy = user.UpdatedBy,
            offboardDate = user.OffboardDate,
            notes = user.Notes
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateUser([FromBody] User input)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var companyId = BE_QLKH.Services.TenantContext.GetCompanyIdOrThrow(User);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd");

        input.Id = ObjectId.GenerateNewId().ToString();
        input.CompanyId = companyId;
        input.CreatedAt = string.IsNullOrEmpty(input.CreatedAt) ? now : input.CreatedAt;
        input.UpdatedAt = input.CreatedAt;

        if (string.IsNullOrWhiteSpace(input.RoleCode))
        {
            return BadRequest(new { message = "Thiếu chức danh" });
        }

        var roleCode = input.RoleCode.Trim().ToLowerInvariant();
        var roleExists = await _roles.Find(r => r.Code == roleCode && r.IsActive).AnyAsync();
        if (!roleExists)
        {
            return BadRequest(new { message = "Chức danh không tồn tại hoặc đã bị tắt" });
        }

        input.RoleCode = roleCode;

        if (string.IsNullOrWhiteSpace(input.PasswordHash))
        {
            var pwd = string.IsNullOrWhiteSpace(input.Password) ? "123456" : input.Password;
            input.PasswordHash = HashPassword(pwd);
        }

        var maxLegacyId = await _users.Find(_ => true)
            .SortByDescending(u => u.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        if (!string.IsNullOrEmpty(input.OffboardDate))
        {
            input.Status = "inactive";
        }

        await _users.InsertOneAsync(input);

        if (!string.IsNullOrWhiteSpace(input.CompanyId))
        {
            var exists = await _userCompanies.Find(x => x.UserLegacyId == input.LegacyId && x.CompanyId == input.CompanyId).AnyAsync();
            if (!exists)
            {
                await _userCompanies.InsertOneAsync(new UserCompany
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    UserLegacyId = input.LegacyId,
                    CompanyId = input.CompanyId,
                    RoleCode = string.IsNullOrWhiteSpace(input.RoleCode) ? null : input.RoleCode,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        return Ok(new
        {
            id = input.LegacyId,
            userId = input.UserId,
            employeeCode = input.EmployeeCode,
            employmentType = input.EmploymentType,
            username = input.Username,
            email = input.Email,
            fullName = input.FullName,
            phone = input.Phone,
            address = input.Address,
            dob = input.Dob,
            idNumber = input.IdNumber,
            idIssuedDate = input.IdIssuedDate,
            idIssuedPlace = input.IdIssuedPlace,
            personalTaxCode = input.PersonalTaxCode,
            bankName = input.BankName,
            bankAccount = input.BankAccount,
            socialInsuranceNumber = input.SocialInsuranceNumber,
            healthInsuranceNumber = input.HealthInsuranceNumber,
            role = input.RoleCode,
            company = input.Company,
            companyId = input.CompanyId,
            department = input.Department,
            position = input.Position,
            status = input.Status,
            avatar = input.Avatar,
            joinDate = input.JoinDate,
            salary = input.Salary,
            contractType = input.ContractType,
            contractStartDate = input.ContractStartDate,
            contractEndDate = input.ContractEndDate,
            workLocation = input.WorkLocation,
            managerName = input.ManagerName,
            managerId = input.ManagerId,
            emergencyContactName = input.EmergencyContactName,
            emergencyContactPhone = input.EmergencyContactPhone,
            group = input.Group,
            dataScope = input.DataScope,
            directPermission = input.DirectPermission,
            approveRight = input.ApproveRight,
            financeViewRight = input.FinanceViewRight,
            exportDataRight = input.ExportDataRight,
            createdAt = input.CreatedAt,
            createdBy = input.CreatedBy,
            updatedAt = input.UpdatedAt,
            updatedBy = input.UpdatedBy,
            offboardDate = input.OffboardDate,
            notes = input.Notes
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateUser(int id, [FromBody] User input)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        input.PasswordHash = user.PasswordHash;
        input.Id = user.Id;
        input.LegacyId = user.LegacyId;
        input.CreatedAt = user.CreatedAt;
        input.CreatedBy = user.CreatedBy;
        input.CompanyId = string.IsNullOrWhiteSpace(input.CompanyId) ? user.CompanyId : input.CompanyId;
        input.Company = string.IsNullOrWhiteSpace(input.Company) ? user.Company : input.Company;
        input.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (string.IsNullOrWhiteSpace(input.RoleCode))
        {
            return BadRequest(new { message = "Thiếu chức danh" });
        }

        var roleCode = input.RoleCode.Trim().ToLowerInvariant();
        var roleExists = await _roles.Find(r => r.Code == roleCode && r.IsActive).AnyAsync();
        if (!roleExists)
        {
            return BadRequest(new { message = "Chức danh không tồn tại hoặc đã bị tắt" });
        }

        input.RoleCode = roleCode;

        if (!string.IsNullOrEmpty(input.OffboardDate))
        {
            input.Status = "inactive";
        }

        await _users.ReplaceOneAsync(u => u.Id == user.Id, input);

        return Ok(new
        {
            id = input.LegacyId,
            userId = input.UserId,
            employeeCode = input.EmployeeCode,
            employmentType = input.EmploymentType,
            username = input.Username,
            email = input.Email,
            fullName = input.FullName,
            phone = input.Phone,
            address = input.Address,
            dob = input.Dob,
            idNumber = input.IdNumber,
            idIssuedDate = input.IdIssuedDate,
            idIssuedPlace = input.IdIssuedPlace,
            personalTaxCode = input.PersonalTaxCode,
            bankName = input.BankName,
            bankAccount = input.BankAccount,
            socialInsuranceNumber = input.SocialInsuranceNumber,
            healthInsuranceNumber = input.HealthInsuranceNumber,
            role = input.RoleCode,
            company = input.Company,
            companyId = input.CompanyId,
            department = input.Department,
            position = input.Position,
            status = input.Status,
            avatar = input.Avatar,
            joinDate = input.JoinDate,
            salary = input.Salary,
            contractType = input.ContractType,
            contractStartDate = input.ContractStartDate,
            contractEndDate = input.ContractEndDate,
            workLocation = input.WorkLocation,
            managerName = input.ManagerName,
            managerId = input.ManagerId,
            emergencyContactName = input.EmergencyContactName,
            emergencyContactPhone = input.EmergencyContactPhone,
            group = input.Group,
            dataScope = input.DataScope,
            directPermission = input.DirectPermission,
            approveRight = input.ApproveRight,
            financeViewRight = input.FinanceViewRight,
            exportDataRight = input.ExportDataRight,
            createdAt = input.CreatedAt,
            createdBy = input.CreatedBy,
            updatedAt = input.UpdatedAt,
            updatedBy = input.UpdatedBy,
            offboardDate = input.OffboardDate,
            notes = input.Notes
        });
    }

    public class UpdateCompanyRequest
    {
        public string CompanyId { get; set; } = string.Empty;
    }

    [HttpPut("{id:int}/company")]
    public async Task<ActionResult<object>> UpdateUserCompany(int id, [FromBody] UpdateCompanyRequest req)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        if (string.IsNullOrWhiteSpace(req.CompanyId)) return BadRequest(new { message = "Thiếu companyId" });

        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        var company = await _companies.Find(c => c.Id == req.CompanyId).FirstOrDefaultAsync();
        if (company == null) return BadRequest(new { message = "Công ty không tồn tại" });

        var update = Builders<User>.Update
            .Set(u => u.CompanyId, company.Id)
            .Set(u => u.Company, company.Name)
            .Set(u => u.UpdatedAt, DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await _users.UpdateOneAsync(u => u.Id == user.Id, update);

        await _userCompanies.DeleteManyAsync(x => x.UserLegacyId == id);
        await _userCompanies.InsertOneAsync(new UserCompany
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserLegacyId = id,
            CompanyId = company.Id,
            RoleCode = string.IsNullOrWhiteSpace(user.RoleCode) ? null : user.RoleCode,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });

        return Ok(new { message = "OK" });
    }

    public class UpdateUserCompaniesRequest
    {
        public List<string> CompanyIds { get; set; } = new();
        public string? DefaultCompanyId { get; set; }
    }

    [HttpPut("{id:int}/companies")]
    public async Task<ActionResult<object>> UpdateUserCompanies(int id, [FromBody] UpdateUserCompaniesRequest req)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        var companyIds = (req.CompanyIds ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (companyIds.Count == 0) return BadRequest(new { message = "Phải chọn ít nhất 1 công ty" });

        var existingCompanies = await _companies.Find(c => companyIds.Contains(c.Id)).ToListAsync();
        if (existingCompanies.Count != companyIds.Count) return BadRequest(new { message = "Danh sách công ty không hợp lệ" });

        var defaultCompanyId = req.DefaultCompanyId;
        if (string.IsNullOrWhiteSpace(defaultCompanyId) || !companyIds.Contains(defaultCompanyId))
        {
            defaultCompanyId = companyIds[0];
        }

        var defaultCompany = existingCompanies.First(c => c.Id == defaultCompanyId);

        await _userCompanies.DeleteManyAsync(x => x.UserLegacyId == id);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        await _userCompanies.InsertManyAsync(companyIds.Select(cid => new UserCompany
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserLegacyId = id,
            CompanyId = cid,
            RoleCode = string.IsNullOrWhiteSpace(user.RoleCode) ? null : user.RoleCode,
            IsDefault = cid == defaultCompanyId,
            CreatedAt = now
        }).ToList());

        var userUpdate = Builders<User>.Update
            .Set(u => u.CompanyId, defaultCompany.Id)
            .Set(u => u.Company, defaultCompany.Name)
            .Set(u => u.UpdatedAt, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        await _users.UpdateOneAsync(u => u.Id == user.Id, userUpdate);

        return Ok(new { message = "OK" });
    }

    [HttpGet("{id:int}/companies")]
    public async Task<ActionResult<object>> GetUserCompanies(int id)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var mappings = await _userCompanies.Find(x => x.UserLegacyId == id).ToListAsync();
        var companyIds = mappings.Select(x => x.CompanyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var companies = companyIds.Count == 0 ? new List<Company>() : await _companies.Find(c => companyIds.Contains(c.Id)).ToListAsync();
        return Ok(new
        {
            items = companies.Select(c => new { id = c.Id, code = c.Code, name = c.Name }).OrderBy(x => x.code).ToList(),
            defaultCompanyId = mappings.FirstOrDefault(x => x.IsDefault)?.CompanyId
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteUser(int id)
    {
        if (!CanManageUsersCompanies())
        {
            return StatusCode(403, new { message = "Bạn không có quyền" });
        }

        var result = await _users.DeleteOneAsync(u => u.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "User not found" });
        return Ok(new { message = "User deleted" });
    }

    private static string HashPassword(string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = SHA256.HashData(passwordBytes);
        return Convert.ToHexString(hashBytes);
    }

    private bool CanManageUsersCompanies()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "assistant_ceo", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "hr", StringComparison.OrdinalIgnoreCase);
    }
}
