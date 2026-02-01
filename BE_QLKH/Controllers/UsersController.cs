using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMongoCollection<User> _users;

    public UsersController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _users = db.GetCollection<User>("users");
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

        var builder = Builders<User>.Filter;
        var filter = builder.Empty;

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
        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

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
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd");

        input.Id = ObjectId.GenerateNewId().ToString();
        input.CreatedAt = string.IsNullOrEmpty(input.CreatedAt) ? now : input.CreatedAt;
        input.UpdatedAt = input.CreatedAt;

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
        var user = await _users.Find(u => u.LegacyId == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "User not found" });

        input.PasswordHash = user.PasswordHash;
        input.Id = user.Id;
        input.LegacyId = user.LegacyId;
        input.CreatedAt = user.CreatedAt;
        input.CreatedBy = user.CreatedBy;
        input.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");

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

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteUser(int id)
    {
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
}
