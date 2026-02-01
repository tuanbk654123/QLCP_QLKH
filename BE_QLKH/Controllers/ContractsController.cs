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
public class ContractsController : ControllerBase
{
    private readonly IMongoCollection<Contract> _contracts;

    public ContractsController(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var db = client.GetDatabase(options.Value.DatabaseName);
        _contracts = db.GetCollection<Contract>("contracts");
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetContracts([FromQuery] string? search, [FromQuery] int page = 1)
    {
        if (page < 1) page = 1;

        var filter = Builders<Contract>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowered = search.ToLower();
            filter &= Builders<Contract>.Filter.Or(
                Builders<Contract>.Filter.Where(c => c.ContractNumber.ToLower().Contains(lowered)),
                Builders<Contract>.Filter.Where(c => c.ContractType.ToLower().Contains(lowered)),
                Builders<Contract>.Filter.Where(c => c.PartyA.Name.ToLower().Contains(lowered)),
                Builders<Contract>.Filter.Where(c => c.PartyB.Name.ToLower().Contains(lowered))
            );
        }

        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var total = await _contracts.CountDocumentsAsync(filter);
        var contracts = await _contracts
            .Find(filter)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();

        var result = contracts.Select(c => new
        {
            id = c.LegacyId,
            contractNumber = c.ContractNumber,
            contractType = c.ContractType,
            partyA = new
            {
                name = c.PartyA.Name,
                taxCode = c.PartyA.TaxCode,
                address = c.PartyA.Address,
                representative = c.PartyA.Representative,
                position = c.PartyA.Position,
                phone = c.PartyA.Phone,
                email = c.PartyA.Email
            },
            partyB = new
            {
                name = c.PartyB.Name,
                idCard = c.PartyB.IdCard,
                taxCode = c.PartyB.TaxCode,
                address = c.PartyB.Address,
                position = c.PartyB.Position,
                phone = c.PartyB.Phone,
                email = c.PartyB.Email
            },
            contractDate = c.ContractDate,
            effectiveDate = c.EffectiveDate,
            expiryDate = c.ExpiryDate,
            terms = c.Terms,
            salary = c.Salary,
            workingHours = c.WorkingHours,
            jobDescription = c.JobDescription,
            serviceDescription = c.ServiceDescription,
            serviceFee = c.ServiceFee,
            paymentTerms = c.PaymentTerms,
            productDescription = c.ProductDescription,
            totalAmount = c.TotalAmount,
            deliveryTerms = c.DeliveryTerms,
            notes = c.Notes,
            status = c.Status
        });

        return Ok(new
        {
            contracts = result,
            contractCount = total
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetContractByLegacyId(int id)
    {
        var contract = await _contracts.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (contract == null) return NotFound(new { message = "Contract not found" });

        return Ok(new
        {
            id = contract.LegacyId,
            contractNumber = contract.ContractNumber,
            contractType = contract.ContractType,
            partyA = new
            {
                name = contract.PartyA.Name,
                taxCode = contract.PartyA.TaxCode,
                address = contract.PartyA.Address,
                representative = contract.PartyA.Representative,
                position = contract.PartyA.Position,
                phone = contract.PartyA.Phone,
                email = contract.PartyA.Email
            },
            partyB = new
            {
                name = contract.PartyB.Name,
                idCard = contract.PartyB.IdCard,
                taxCode = contract.PartyB.TaxCode,
                address = contract.PartyB.Address,
                position = contract.PartyB.Position,
                phone = contract.PartyB.Phone,
                email = contract.PartyB.Email
            },
            contractDate = contract.ContractDate,
            effectiveDate = contract.EffectiveDate,
            expiryDate = contract.ExpiryDate,
            terms = contract.Terms,
            salary = contract.Salary,
            workingHours = contract.WorkingHours,
            jobDescription = contract.JobDescription,
            serviceDescription = contract.ServiceDescription,
            serviceFee = contract.ServiceFee,
            paymentTerms = contract.PaymentTerms,
            productDescription = contract.ProductDescription,
            totalAmount = contract.TotalAmount,
            deliveryTerms = contract.DeliveryTerms,
            notes = contract.Notes,
            status = contract.Status
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateContract([FromBody] Contract input)
    {
        input.Id = ObjectId.GenerateNewId().ToString();

        var maxLegacyId = await _contracts.Find(_ => true)
            .SortByDescending(c => c.LegacyId)
            .Limit(1)
            .FirstOrDefaultAsync();

        input.LegacyId = maxLegacyId != null ? maxLegacyId.LegacyId + 1 : 1;

        await _contracts.InsertOneAsync(input);

        return Ok(new
        {
            id = input.LegacyId,
            contractNumber = input.ContractNumber,
            contractType = input.ContractType,
            partyA = new
            {
                name = input.PartyA.Name,
                taxCode = input.PartyA.TaxCode,
                address = input.PartyA.Address,
                representative = input.PartyA.Representative,
                position = input.PartyA.Position,
                phone = input.PartyA.Phone,
                email = input.PartyA.Email
            },
            partyB = new
            {
                name = input.PartyB.Name,
                idCard = input.PartyB.IdCard,
                taxCode = input.PartyB.TaxCode,
                address = input.PartyB.Address,
                position = input.PartyB.Position,
                phone = input.PartyB.Phone,
                email = input.PartyB.Email
            },
            contractDate = input.ContractDate,
            effectiveDate = input.EffectiveDate,
            expiryDate = input.ExpiryDate,
            terms = input.Terms,
            salary = input.Salary,
            workingHours = input.WorkingHours,
            jobDescription = input.JobDescription,
            serviceDescription = input.ServiceDescription,
            serviceFee = input.ServiceFee,
            paymentTerms = input.PaymentTerms,
            productDescription = input.ProductDescription,
            totalAmount = input.TotalAmount,
            deliveryTerms = input.DeliveryTerms,
            notes = input.Notes,
            status = input.Status
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> UpdateContract(int id, [FromBody] Contract input)
    {
        var contract = await _contracts.Find(c => c.LegacyId == id).FirstOrDefaultAsync();
        if (contract == null) return NotFound(new { message = "Contract not found" });

        input.Id = contract.Id;
        input.LegacyId = contract.LegacyId;

        await _contracts.ReplaceOneAsync(c => c.Id == contract.Id, input);

        return Ok(new
        {
            id = input.LegacyId,
            contractNumber = input.ContractNumber,
            contractType = input.ContractType,
            partyA = new
            {
                name = input.PartyA.Name,
                taxCode = input.PartyA.TaxCode,
                address = input.PartyA.Address,
                representative = input.PartyA.Representative,
                position = input.PartyA.Position,
                phone = input.PartyA.Phone,
                email = input.PartyA.Email
            },
            partyB = new
            {
                name = input.PartyB.Name,
                idCard = input.PartyB.IdCard,
                taxCode = input.PartyB.TaxCode,
                address = input.PartyB.Address,
                position = input.PartyB.Position,
                phone = input.PartyB.Phone,
                email = input.PartyB.Email
            },
            contractDate = input.ContractDate,
            effectiveDate = input.EffectiveDate,
            expiryDate = input.ExpiryDate,
            terms = input.Terms,
            salary = input.Salary,
            workingHours = input.WorkingHours,
            jobDescription = input.JobDescription,
            serviceDescription = input.ServiceDescription,
            serviceFee = input.ServiceFee,
            paymentTerms = input.PaymentTerms,
            productDescription = input.ProductDescription,
            totalAmount = input.TotalAmount,
            deliveryTerms = input.DeliveryTerms,
            notes = input.Notes,
            status = input.Status
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> DeleteContract(int id)
    {
        var result = await _contracts.DeleteOneAsync(c => c.LegacyId == id);
        if (result.DeletedCount == 0) return NotFound(new { message = "Contract not found" });
        return Ok(new { message = "Contract deleted" });
    }
}
