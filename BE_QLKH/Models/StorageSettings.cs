namespace BE_QLKH.Models;

public class StorageSettings
{
    public string Provider { get; set; } = "Local";
    public S3StorageSettings S3 { get; set; } = new();
}

public class S3StorageSettings
{
    public string ServiceUrl { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "qlkh-uploads";
    public string PublicBaseUrl { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; } = true;
}

