namespace BE_QLKH.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<StoredFile> UploadAsync(IFormFile file, string prefix, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0) throw new InvalidOperationException("No file uploaded");

        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/{fileName}";
        return new StoredFile(relativePath, relativePath, file.FileName, file.ContentType ?? "application/octet-stream", file.Length);
    }
}

