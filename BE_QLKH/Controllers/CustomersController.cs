using BE_QLKH.Models;
using BE_QLKH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMongoCollection<Customer> _customers;
    private readonly IMongoCollection<User> _users;
    private readonly IAuditLogService _auditLogService;

    public CustomersController(IMongoClient client, IOptions<MongoDbSettings> options, IAuditLogService auditLogService)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _customers = db.GetCollection<Customer>("customers");
        _users = db.GetCollection<User>("users");
        _auditLogService = auditLogService;
    }

    private int GetActorLegacyId()
    {
        var legacyIdClaim = User.FindFirst("legacy_id")?.Value;
        if (legacyIdClaim != null && int.TryParse(legacyIdClaim, out var legacyId))
        {
            return legacyId;
        }
        return 0;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetCustomers(
        [FromQuery] string? search, 
        [FromQuery] int page = 1,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        if (page < 1) page = 1;

        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        var builder = Builders<Customer>.Filter;
        var filter = TenantContext.CompanyFilter<Customer>(companyId);

        // 1. Global Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Escape special regex characters to prevent errors
            var escapedSearch = System.Text.RegularExpressions.Regex.Escape(search);
            filter &= builder.Or(
                builder.Regex("name", new BsonRegularExpression(escapedSearch, "i")),
                builder.Regex("email", new BsonRegularExpression(escapedSearch, "i")),
                builder.Regex("phone", new BsonRegularExpression(escapedSearch, "i"))
            );
        }

        // 2. Column Filters
        var properties = typeof(Customer).GetProperties();
        foreach (var query in Request.Query)
        {
            var key = query.Key;
            var value = query.Value.ToString();
            
            if (string.IsNullOrEmpty(value)) continue;
            if (new[] { "search", "page", "sortfield", "sortorder", "limit" }.Contains(key.ToLower())) continue;

            // Find matching property (case-insensitive)
            var prop = properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                var bsonAttr = prop.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonElementAttribute), false)
                    .FirstOrDefault() as MongoDB.Bson.Serialization.Attributes.BsonElementAttribute;
                var dbField = bsonAttr?.ElementName ?? prop.Name;
                
                // Use Regex for string contains search (case insensitive)
                // For numeric/date fields, this might need adjustment, but Regex often works if stored as string or casting.
                // Given the model has some ints (TotalOrders) and decimals, regex might fail on them in some mongo versions or be slow.
                // However, user asked for "search", implying text search. 
                // If the field is int, we should probably use Eq or convert input.
                
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
                     // Fallback to regex (works for some types depending on serialization) or ignore
                     // Most fields in Customer.cs are strings.
                     filter &= builder.Regex(dbField, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(value), "i"));
                }
            }
        }

        // 3. Sorting
        SortDefinition<Customer> sort = Builders<Customer>.Sort.Descending("legacy_id"); // Default
        if (!string.IsNullOrEmpty(sortField))
        {
            var prop = properties.FirstOrDefault(p => p.Name.Equals(sortField, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                 var bsonAttr = prop.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonElementAttribute), false)
                    .FirstOrDefault() as MongoDB.Bson.Serialization.Attributes.BsonElementAttribute;
                 var dbField = bsonAttr?.ElementName ?? prop.Name;
                 
                 if (sortOrder?.ToLower() == "asc" || sortOrder?.ToLower() == "ascend")
                     sort = Builders<Customer>.Sort.Ascending(dbField);
                 else
                     sort = Builders<Customer>.Sort.Descending(dbField);
            }
        }

        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var total = await _customers.CountDocumentsAsync(filter);
        var customers = await _customers
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var userIds = customers
            .SelectMany(c => new[] { c.CreatedBy, c.UpdatedBy })
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var users = userIds.Count == 0
            ? new List<User>()
            : await _users.Find(u => userIds.Contains(u.LegacyId)).ToListAsync();

        var userNameMap = users
            .GroupBy(u => u.LegacyId)
            .ToDictionary(g => g.Key, g => g.First().FullName);

        var result = customers.Select(c => new
        {
            id = c.LegacyId,
            name = c.Name,
            email = c.Email,
            phone = c.Phone,
            address = c.Address,
            company = c.Company,
            taxCode = c.TaxCode,
            representativeName = c.RepresentativeName,
            representativePosition = c.RepresentativePosition,
            representativePhone = c.RepresentativePhone,
            idNumber = c.IdNumber,
            contactPerson = c.ContactPerson,
            contactPhone = c.ContactPhone,
            contactEmail = c.ContactEmail,
            businessNeeds = c.BusinessNeeds,
            businessScale = c.BusinessScale,
            businessIndustry = c.BusinessIndustry,
            copyrightStatus = c.CopyrightStatus,
            trademarkStatus = c.TrademarkStatus,
            patentStatus = c.PatentStatus,
            industrialDesign = c.IndustrialDesign,
            contractStatus = c.ContractStatus,
            status = c.Status,
            totalOrders = c.TotalOrders,
            totalRevenue = c.TotalRevenue,
            joinDate = c.JoinDate,
            notes = c.Notes,
            productsServices = c.ProductsServices,
            ipGroup = c.IpGroup,
            brandName = c.BrandName,
            owner = c.Owner,
            protectionTerritory = c.ProtectionTerritory,
            consultingStatus = c.ConsultingStatus,
            filingStatus = c.FilingStatus,
            filingDate = c.FilingDate,
            applicationCode = c.ApplicationCode,
            issueDate = c.IssueDate,
            expiryDate = c.ExpiryDate,
            processingDeadline = c.ProcessingDeadline,
            renewalCycle = c.RenewalCycle,
            renewalDate = c.RenewalDate,
            reminderDate = c.ReminderDate,
            reminderStatus = c.ReminderStatus,
            documentLink = c.DocumentLink,
            authorization = c.Authorization,
            applicationReviewStatus = c.ApplicationReviewStatus,
            priority = c.Priority,
            contractPaid = c.ContractPaid,
            contractNumber = c.ContractNumber,
            contractValue = c.ContractValue,
            stateFee = c.StateFee,
            additionalFee = c.AdditionalFee,
            startDate = c.StartDate,
            endDate = c.EndDate,
            implementationDays = c.ImplementationDays,
            potentialLevel = c.PotentialLevel,
            sourceClassification = c.SourceClassification,
            nsnnSource = c.NsnnSource,
            createdAt = c.CreatedAt,
            createdBy = c.CreatedBy,
            createdByName = userNameMap.TryGetValue(c.CreatedBy, out var cName) ? cName : string.Empty,
            updatedAt = c.UpdatedAt,
            updatedBy = c.UpdatedBy,
            updatedByName = userNameMap.TryGetValue(c.UpdatedBy, out var uName) ? uName : string.Empty
        });

        return Ok(new
        {
            customers = result,
            customerCount = total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetCustomerByLegacyId(int id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var customer = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, id)).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Customer not found" });

        var userIds = new[] { customer.CreatedBy, customer.UpdatedBy }.Where(x => x > 0).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : await _users.Find(u => userIds.Contains(u.LegacyId)).ToListAsync();
        var userNameMap = users
            .GroupBy(u => u.LegacyId)
            .ToDictionary(g => g.Key, g => g.First().FullName);

        return Ok(new
        {
            id = customer.LegacyId,
            name = customer.Name,
            email = customer.Email,
            phone = customer.Phone,
            address = customer.Address,
            company = customer.Company,
            taxCode = customer.TaxCode,
            representativeName = customer.RepresentativeName,
            representativePosition = customer.RepresentativePosition,
            representativePhone = customer.RepresentativePhone,
            idNumber = customer.IdNumber,
            contactPerson = customer.ContactPerson,
            contactPhone = customer.ContactPhone,
            contactEmail = customer.ContactEmail,
            businessNeeds = customer.BusinessNeeds,
            businessScale = customer.BusinessScale,
            businessIndustry = customer.BusinessIndustry,
            copyrightStatus = customer.CopyrightStatus,
            trademarkStatus = customer.TrademarkStatus,
            patentStatus = customer.PatentStatus,
            industrialDesign = customer.IndustrialDesign,
            contractStatus = customer.ContractStatus,
            status = customer.Status,
            totalOrders = customer.TotalOrders,
            totalRevenue = customer.TotalRevenue,
            joinDate = customer.JoinDate,
            notes = customer.Notes,
            productsServices = customer.ProductsServices,
            ipGroup = customer.IpGroup,
            brandName = customer.BrandName,
            owner = customer.Owner,
            protectionTerritory = customer.ProtectionTerritory,
            consultingStatus = customer.ConsultingStatus,
            filingStatus = customer.FilingStatus,
            filingDate = customer.FilingDate,
            applicationCode = customer.ApplicationCode,
            issueDate = customer.IssueDate,
            expiryDate = customer.ExpiryDate,
            processingDeadline = customer.ProcessingDeadline,
            renewalCycle = customer.RenewalCycle,
            renewalDate = customer.RenewalDate,
            reminderDate = customer.ReminderDate,
            reminderStatus = customer.ReminderStatus,
            documentLink = customer.DocumentLink,
            authorization = customer.Authorization,
            applicationReviewStatus = customer.ApplicationReviewStatus,
            priority = customer.Priority,
            contractPaid = customer.ContractPaid,
            contractNumber = customer.ContractNumber,
            contractValue = customer.ContractValue,
            stateFee = customer.StateFee,
            additionalFee = customer.AdditionalFee,
            startDate = customer.StartDate,
            endDate = customer.EndDate,
            implementationDays = customer.ImplementationDays,
            potentialLevel = customer.PotentialLevel,
            sourceClassification = customer.SourceClassification,
            nsnnSource = customer.NsnnSource,
            createdAt = customer.CreatedAt,
            createdBy = customer.CreatedBy,
            createdByName = userNameMap.TryGetValue(customer.CreatedBy, out var cName) ? cName : string.Empty,
            updatedAt = customer.UpdatedAt,
            updatedBy = customer.UpdatedBy,
            updatedByName = userNameMap.TryGetValue(customer.UpdatedBy, out var uName) ? uName : string.Empty
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateCustomer([FromBody] Customer input)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var now = DateTime.Now.ToString("yyyy-MM-dd");
        var actorId = GetActorLegacyId();

        input.Id = ObjectId.GenerateNewId().ToString();
        input.CompanyId = companyId;
        input.CreatedAt = now;
        input.CreatedBy = actorId;
        input.UpdatedAt = now;
        input.UpdatedBy = actorId;

        var maxLegacyId = await _customers.Find(_ => true)
            .SortByDescending(c => c.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        await _customers.InsertOneAsync(input);

        var actor = actorId > 0 ? await _users.Find(u => u.LegacyId == actorId).FirstOrDefaultAsync() : null;
        var actorName = actor?.FullName ?? string.Empty;
        await _auditLogService.LogAsync("customer", input.LegacyId, "create", actor, null, input);

        return Ok(new
        {
            id = input.LegacyId,
            name = input.Name,
            email = input.Email,
            phone = input.Phone,
            address = input.Address,
            company = input.Company,
            taxCode = input.TaxCode,
            representativeName = input.RepresentativeName,
            representativePosition = input.RepresentativePosition,
            representativePhone = input.RepresentativePhone,
            idNumber = input.IdNumber,
            contactPerson = input.ContactPerson,
            contactPhone = input.ContactPhone,
            contactEmail = input.ContactEmail,
            businessNeeds = input.BusinessNeeds,
            businessScale = input.BusinessScale,
            businessIndustry = input.BusinessIndustry,
            copyrightStatus = input.CopyrightStatus,
            trademarkStatus = input.TrademarkStatus,
            patentStatus = input.PatentStatus,
            industrialDesign = input.IndustrialDesign,
            contractStatus = input.ContractStatus,
            status = input.Status,
            totalOrders = input.TotalOrders,
            totalRevenue = input.TotalRevenue,
            joinDate = input.JoinDate,
            notes = input.Notes,
            productsServices = input.ProductsServices,
            ipGroup = input.IpGroup,
            brandName = input.BrandName,
            owner = input.Owner,
            protectionTerritory = input.ProtectionTerritory,
            consultingStatus = input.ConsultingStatus,
            filingStatus = input.FilingStatus,
            filingDate = input.FilingDate,
            applicationCode = input.ApplicationCode,
            issueDate = input.IssueDate,
            expiryDate = input.ExpiryDate,
            processingDeadline = input.ProcessingDeadline,
            renewalCycle = input.RenewalCycle,
            renewalDate = input.RenewalDate,
            reminderDate = input.ReminderDate,
            reminderStatus = input.ReminderStatus,
            documentLink = input.DocumentLink,
            authorization = input.Authorization,
            applicationReviewStatus = input.ApplicationReviewStatus,
            priority = input.Priority,
            contractPaid = input.ContractPaid,
            contractNumber = input.ContractNumber,
            contractValue = input.ContractValue,
            stateFee = input.StateFee,
            additionalFee = input.AdditionalFee,
            startDate = input.StartDate,
            endDate = input.EndDate,
            implementationDays = input.ImplementationDays,
            potentialLevel = input.PotentialLevel,
            sourceClassification = input.SourceClassification,
            nsnnSource = input.NsnnSource,
            createdAt = input.CreatedAt,
            createdBy = input.CreatedBy,
            createdByName = actorName,
            updatedAt = input.UpdatedAt,
            updatedBy = input.UpdatedBy,
            updatedByName = actorName
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateCustomer(int id, [FromBody] Customer input)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var customer = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, id)).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Customer not found" });

        var before = JsonSerializer.Deserialize<Customer>(JsonSerializer.Serialize(customer)) ?? customer;

        var now = DateTime.Now.ToString("yyyy-MM-dd");
        var actorId = GetActorLegacyId();

        customer.Name = input.Name ?? customer.Name;
        customer.Email = input.Email ?? customer.Email;
        customer.Phone = input.Phone ?? customer.Phone;
        customer.Address = input.Address ?? customer.Address;
        customer.Company = input.Company ?? customer.Company;
        customer.TaxCode = input.TaxCode ?? customer.TaxCode;
        customer.RepresentativeName = input.RepresentativeName ?? customer.RepresentativeName;
        customer.RepresentativePosition = input.RepresentativePosition ?? customer.RepresentativePosition;
        customer.RepresentativePhone = input.RepresentativePhone ?? customer.RepresentativePhone;
        customer.IdNumber = input.IdNumber ?? customer.IdNumber;
        customer.ContactPerson = input.ContactPerson ?? customer.ContactPerson;
        customer.ContactPhone = input.ContactPhone ?? customer.ContactPhone;
        customer.ContactEmail = input.ContactEmail ?? customer.ContactEmail;
        customer.BusinessNeeds = input.BusinessNeeds ?? customer.BusinessNeeds;
        customer.BusinessScale = input.BusinessScale ?? customer.BusinessScale;
        customer.BusinessIndustry = input.BusinessIndustry ?? customer.BusinessIndustry;
        customer.CopyrightStatus = input.CopyrightStatus ?? customer.CopyrightStatus;
        customer.TrademarkStatus = input.TrademarkStatus ?? customer.TrademarkStatus;
        customer.PatentStatus = input.PatentStatus ?? customer.PatentStatus;
        customer.IndustrialDesign = input.IndustrialDesign ?? customer.IndustrialDesign;
        customer.ContractStatus = input.ContractStatus ?? customer.ContractStatus;
        customer.Status = input.Status ?? customer.Status;
        customer.TotalOrders = input.TotalOrders ?? customer.TotalOrders;
        customer.TotalRevenue = input.TotalRevenue ?? customer.TotalRevenue;
        customer.JoinDate = input.JoinDate ?? customer.JoinDate;
        customer.Notes = input.Notes ?? customer.Notes;
        customer.ProductsServices = input.ProductsServices ?? customer.ProductsServices;
        customer.IpGroup = input.IpGroup ?? customer.IpGroup;
        customer.BrandName = input.BrandName ?? customer.BrandName;
        customer.Owner = input.Owner ?? customer.Owner;
        customer.ProtectionTerritory = input.ProtectionTerritory ?? customer.ProtectionTerritory;
        customer.ConsultingStatus = input.ConsultingStatus ?? customer.ConsultingStatus;
        customer.FilingStatus = input.FilingStatus ?? customer.FilingStatus;
        customer.FilingDate = input.FilingDate ?? customer.FilingDate;
        customer.ApplicationCode = input.ApplicationCode ?? customer.ApplicationCode;
        customer.IssueDate = input.IssueDate ?? customer.IssueDate;
        customer.ExpiryDate = input.ExpiryDate ?? customer.ExpiryDate;
        customer.ProcessingDeadline = input.ProcessingDeadline ?? customer.ProcessingDeadline;
        customer.RenewalCycle = input.RenewalCycle ?? customer.RenewalCycle;
        customer.RenewalDate = input.RenewalDate ?? customer.RenewalDate;
        customer.ReminderDate = input.ReminderDate ?? customer.ReminderDate;
        customer.ReminderStatus = input.ReminderStatus ?? customer.ReminderStatus;
        customer.DocumentLink = input.DocumentLink ?? customer.DocumentLink;
        customer.Authorization = input.Authorization ?? customer.Authorization;
        customer.ApplicationReviewStatus = input.ApplicationReviewStatus ?? customer.ApplicationReviewStatus;
        customer.Priority = input.Priority ?? customer.Priority;
        customer.ContractPaid = input.ContractPaid ?? customer.ContractPaid;
        customer.ContractNumber = input.ContractNumber ?? customer.ContractNumber;
        customer.ContractValue = input.ContractValue ?? customer.ContractValue;
        customer.StateFee = input.StateFee ?? customer.StateFee;
        customer.AdditionalFee = input.AdditionalFee ?? customer.AdditionalFee;
        customer.StartDate = input.StartDate ?? customer.StartDate;
        customer.EndDate = input.EndDate ?? customer.EndDate;
        customer.ImplementationDays = input.ImplementationDays ?? customer.ImplementationDays;
        customer.PotentialLevel = input.PotentialLevel ?? customer.PotentialLevel;
        customer.SourceClassification = input.SourceClassification ?? customer.SourceClassification;
        customer.NsnnSource = input.NsnnSource ?? customer.NsnnSource;

        customer.UpdatedAt = now;
        customer.UpdatedBy = actorId;

        await _customers.ReplaceOneAsync(c => c.Id == customer.Id, customer);

        var creator = customer.CreatedBy > 0 ? await _users.Find(u => u.LegacyId == customer.CreatedBy).FirstOrDefaultAsync() : null;
        var updater = customer.UpdatedBy > 0 ? await _users.Find(u => u.LegacyId == customer.UpdatedBy).FirstOrDefaultAsync() : null;
        await _auditLogService.LogAsync("customer", customer.LegacyId, "update", updater, before, customer);

        return Ok(new
        {
            id = customer.LegacyId,
            name = customer.Name,
            email = customer.Email,
            phone = customer.Phone,
            address = customer.Address,
            company = customer.Company,
            taxCode = customer.TaxCode,
            representativeName = customer.RepresentativeName,
            representativePosition = customer.RepresentativePosition,
            representativePhone = customer.RepresentativePhone,
            idNumber = customer.IdNumber,
            contactPerson = customer.ContactPerson,
            contactPhone = customer.ContactPhone,
            contactEmail = customer.ContactEmail,
            businessNeeds = customer.BusinessNeeds,
            businessScale = customer.BusinessScale,
            businessIndustry = customer.BusinessIndustry,
            copyrightStatus = customer.CopyrightStatus,
            trademarkStatus = customer.TrademarkStatus,
            patentStatus = customer.PatentStatus,
            industrialDesign = customer.IndustrialDesign,
            contractStatus = customer.ContractStatus,
            status = customer.Status,
            totalOrders = customer.TotalOrders,
            totalRevenue = customer.TotalRevenue,
            joinDate = customer.JoinDate,
            notes = customer.Notes,
            productsServices = customer.ProductsServices,
            ipGroup = customer.IpGroup,
            brandName = customer.BrandName,
            owner = customer.Owner,
            protectionTerritory = customer.ProtectionTerritory,
            consultingStatus = customer.ConsultingStatus,
            filingStatus = customer.FilingStatus,
            filingDate = customer.FilingDate,
            applicationCode = customer.ApplicationCode,
            issueDate = customer.IssueDate,
            expiryDate = customer.ExpiryDate,
            processingDeadline = customer.ProcessingDeadline,
            renewalCycle = customer.RenewalCycle,
            renewalDate = customer.RenewalDate,
            reminderDate = customer.ReminderDate,
            reminderStatus = customer.ReminderStatus,
            documentLink = customer.DocumentLink,
            authorization = customer.Authorization,
            applicationReviewStatus = customer.ApplicationReviewStatus,
            priority = customer.Priority,
            contractPaid = customer.ContractPaid,
            contractNumber = customer.ContractNumber,
            contractValue = customer.ContractValue,
            stateFee = customer.StateFee,
            additionalFee = customer.AdditionalFee,
            startDate = customer.StartDate,
            endDate = customer.EndDate,
            implementationDays = customer.ImplementationDays,
            potentialLevel = customer.PotentialLevel,
            sourceClassification = customer.SourceClassification,
            nsnnSource = customer.NsnnSource,
            createdAt = customer.CreatedAt,
            createdBy = customer.CreatedBy,
            createdByName = creator?.FullName ?? string.Empty,
            updatedAt = customer.UpdatedAt,
            updatedBy = customer.UpdatedBy,
            updatedByName = updater?.FullName ?? string.Empty
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteCustomer(int id)
    {
        var companyId = TenantContext.GetCompanyIdOrThrow(User);
        var customer = await _customers.Find(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, id)).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Customer not found" });

        var actorId = GetActorLegacyId();
        var actor = actorId > 0 ? await _users.Find(u => u.LegacyId == actorId).FirstOrDefaultAsync() : null;

        var result = await _customers.DeleteOneAsync(TenantContext.CompanyFilter<Customer>(companyId) & Builders<Customer>.Filter.Eq(c => c.LegacyId, id));
        if (result.DeletedCount == 0) return NotFound(new { message = "Customer not found" });
        await _auditLogService.LogAsync("customer", id, "delete", actor, customer, null);
        return Ok(new { message = "Customer deleted" });
    }
}
