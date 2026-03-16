namespace BE_QLKH.Services;

public record StoredFile(string Key, string Url, string OriginalName, string ContentType, long Size);

public interface IFileStorage
{
    Task<StoredFile> UploadAsync(IFormFile file, string prefix, CancellationToken cancellationToken = default);
}

