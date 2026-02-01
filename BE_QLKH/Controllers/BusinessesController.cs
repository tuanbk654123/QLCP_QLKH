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
public class BusinessesController : ControllerBase
{
    private readonly IMongoCollection<Business> _businesses;

    public BusinessesController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _businesses = db.GetCollection<Business>("businesses");
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetBusinesses([FromQuery] string? search, [FromQuery] int page = 1)
    {
        if (page < 1) page = 1;

        var filter = Builders<Business>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowered = search.ToLower();
            filter &= Builders<Business>.Filter.Or(
                Builders<Business>.Filter.Where(b => b.Name.ToLower().Contains(lowered)),
                Builders<Business>.Filter.Where(b => b.TaxCode.Contains(search)),
                Builders<Business>.Filter.Where(b => b.ContactPerson.ToLower().Contains(lowered))
            );
        }

        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var total = await _businesses.CountDocumentsAsync(filter);
        var businesses = await _businesses
            .Find(filter)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var result = businesses.Select(b => new
        {
            id = b.LegacyId,
            name = b.Name,
            taxCode = b.TaxCode,
            address = b.Address,
            phone = b.Phone,
            email = b.Email,
            website = b.Website,
            industry = b.Industry,
            employeeCount = b.EmployeeCount,
            revenue = b.Revenue,
            status = b.Status,
            contactPerson = b.ContactPerson,
            contactPhone = b.ContactPhone,
            establishedDate = b.EstablishedDate,
            notes = b.Notes
        });

        return Ok(new
        {
            businesses = result,
            businessCount = total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetBusinessByLegacyId(int id)
    {
        var business = await _businesses.Find(b => b.LegacyId == id).FirstOrDefaultAsync();
        if (business == null) return NotFound(new { message = "Business not found" });

        return Ok(new
        {
            id = business.LegacyId,
            name = business.Name,
            taxCode = business.TaxCode,
            address = business.Address,
            phone = business.Phone,
            email = business.Email,
            website = business.Website,
            industry = business.Industry,
            employeeCount = business.EmployeeCount,
            revenue = business.Revenue,
            status = business.Status,
            contactPerson = business.ContactPerson,
            contactPhone = business.ContactPhone,
            establishedDate = business.EstablishedDate,
            notes = business.Notes
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateBusiness([FromBody] Business input)
    {
        input.Id = ObjectId.GenerateNewId().ToString();

        var maxLegacyId = await _businesses.Find(_ => true)
            .SortByDescending(b => b.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        await _businesses.InsertOneAsync(input);

        return Ok(new
        {
            id = input.LegacyId,
            name = input.Name,
            taxCode = input.TaxCode,
            address = input.Address,
            phone = input.Phone,
            email = input.Email,
            website = input.Website,
            industry = input.Industry,
            employeeCount = input.EmployeeCount,
            revenue = input.Revenue,
            status = input.Status,
            contactPerson = input.ContactPerson,
            contactPhone = input.ContactPhone,
            establishedDate = input.EstablishedDate,
            notes = input.Notes
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateBusiness(int id, [FromBody] Business input)
    {
        var business = await _businesses.Find(b => b.LegacyId == id).FirstOrDefaultAsync();
        if (business == null) return NotFound(new { message = "Business not found" });

        input.Id = business.Id;
        input.LegacyId = business.LegacyId;

        await _businesses.ReplaceOneAsync(b => b.Id == business.Id, input);

        return Ok(new
        {
            id = input.LegacyId,
            name = input.Name,
            taxCode = input.TaxCode,
            address = input.Address,
            phone = input.Phone,
            email = input.Email,
            website = input.Website,
            industry = input.Industry,
            employeeCount = input.EmployeeCount,
            revenue = input.Revenue,
            status = input.Status,
            contactPerson = input.ContactPerson,
            contactPhone = input.ContactPhone,
            establishedDate = input.EstablishedDate,
            notes = input.Notes
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteBusiness(int id)
    {
        var result = await _businesses.DeleteOneAsync(b => b.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "Business not found" });
        return Ok(new { message = "Business deleted" });
    }
}
