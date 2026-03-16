using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BE_QLKH.Models;
using Microsoft.Extensions.Options;

namespace BE_QLKH.Services;

public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly StorageSettings _settings;

    public S3FileStorage(IAmazonS3 s3, IOptions<StorageSettings> settings)
    {
        _s3 = s3;
        _settings = settings.Value;
    }

    public async Task<StoredFile> UploadAsync(IFormFile file, string prefix, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0) throw new InvalidOperationException("No file uploaded");

        var ext = Path.GetExtension(file.FileName);
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "uploads" : prefix.Trim().Replace("\\", "/").Trim('/');
        var key = $"{safePrefix}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{ext}";

        await using var input = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _settings.S3.Bucket,
            Key = key,
            InputStream = input,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
        };

        try
        {
            await _s3.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _settings.S3.Bucket }, cancellationToken);
            await _s3.PutObjectAsync(request, cancellationToken);
        }

        var baseUrl = _settings.S3.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = _settings.S3.ServiceUrl;
        }

        baseUrl = baseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{_settings.S3.Bucket}/{key}";

        return new StoredFile(key, url, file.FileName, request.ContentType, file.Length);
    }
}

