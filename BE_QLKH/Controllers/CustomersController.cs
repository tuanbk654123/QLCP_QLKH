using BE_QLKH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMongoCollection<Customer> _customers;

    public CustomersController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _customers = db.GetCollection<Customer>("customers");
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetCustomers(
        [FromQuery] string? search, 
        [FromQuery] int page = 1,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortOrder = null)
    {
        if (page < 1) page = 1;

        var builder = Builders<Customer>.Filter;
        var filter = builder.Empty;

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
            consultingStatus = c.ConsultingStatus,
            filingStatus = c.FilingStatus,
            documentLink = c.DocumentLink,
            authorization = c.Authorization,
            applicationReviewStatus = c.ApplicationReviewStatus,
            priority = c.Priority,
            contractPaid = c.ContractPaid,
            contractValue = c.ContractValue,
            startDate = c.StartDate,
            endDate = c.EndDate,
            implementationDays = c.ImplementationDays,
            potentialLevel = c.PotentialLevel,
            sourceClassification = c.SourceClassification,
            nsnnSource = c.NsnnSource
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
        var customer = await _customers.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Customer not found" });

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
            consultingStatus = customer.ConsultingStatus,
            filingStatus = customer.FilingStatus,
            documentLink = customer.DocumentLink,
            authorization = customer.Authorization,
            applicationReviewStatus = customer.ApplicationReviewStatus,
            priority = customer.Priority,
            contractPaid = customer.ContractPaid,
            contractValue = customer.ContractValue,
            startDate = customer.StartDate,
            endDate = customer.EndDate,
            implementationDays = customer.ImplementationDays,
            potentialLevel = customer.PotentialLevel,
            sourceClassification = customer.SourceClassification,
            nsnnSource = customer.NsnnSource
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateCustomer([FromBody] Customer input)
    {
        input.Id = ObjectId.GenerateNewId().ToString();

        var maxLegacyId = await _customers.Find(_ => true)
            .SortByDescending(c => c.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        await _customers.InsertOneAsync(input);

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
            consultingStatus = input.ConsultingStatus,
            filingStatus = input.FilingStatus,
            documentLink = input.DocumentLink,
            authorization = input.Authorization,
            applicationReviewStatus = input.ApplicationReviewStatus,
            priority = input.Priority,
            contractPaid = input.ContractPaid,
            contractValue = input.ContractValue,
            startDate = input.StartDate,
            endDate = input.EndDate,
            implementationDays = input.ImplementationDays,
            potentialLevel = input.PotentialLevel,
            sourceClassification = input.SourceClassification,
            nsnnSource = input.NsnnSource
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateCustomer(int id, [FromBody] Customer input)
    {
        var customer = await _customers.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Customer not found" });

        input.Id = customer.Id;
        input.LegacyId = customer.LegacyId;

        await _customers.ReplaceOneAsync(c => c.Id == customer.Id, input);

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
            consultingStatus = input.ConsultingStatus,
            filingStatus = input.FilingStatus,
            documentLink = input.DocumentLink,
            authorization = input.Authorization,
            applicationReviewStatus = input.ApplicationReviewStatus,
            priority = input.Priority,
            contractPaid = input.ContractPaid,
            contractValue = input.ContractValue,
            startDate = input.StartDate,
            endDate = input.EndDate,
            implementationDays = input.ImplementationDays,
            potentialLevel = input.PotentialLevel,
            sourceClassification = input.SourceClassification,
            nsnnSource = input.NsnnSource
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteCustomer(int id)
    {
        var result = await _customers.DeleteOneAsync(c => c.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "Customer not found" });
        return Ok(new { message = "Customer deleted" });
    }
}
