using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE_QLKH.Services;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public UploadController(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpPost]
    public async Task<ActionResult<object>> Upload(IFormFile file, [FromQuery] string? module)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var companyId = TenantContext.GetCompanyIdOrThrow(User);

        var prefix = "uploads";
        var m = module?.Trim().ToLowerInvariant();
        if (m == "qlcp" || m == "cost" || m == "costs") prefix = "costs";
        if (m == "qlkh" || m == "customer" || m == "customers") prefix = "customers";
        prefix = $"{prefix}/{companyId}";

        var stored = await _fileStorage.UploadAsync(file, prefix, HttpContext.RequestAborted);
        return Ok(new { path = stored.Url, key = stored.Key, originalName = stored.OriginalName });
    }
}
