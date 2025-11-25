using Minio;
using Minio.DataModel.Args;
using System.Net;
using WishLister.Utils;

namespace WishLister.Services;
public class MinIOService
{
    private readonly MinioClient _minioClient;
    private readonly string _bucketName;

    public MinIOService()
    {
        var endpoint = ConfigHelper.GetMinIoEndpoint();
        var accessKey = ConfigHelper.GetMinIoAccessKey();
        var secretKey = ConfigHelper.GetMinIoSecretKey();
        _bucketName = ConfigHelper.GetMinIoBucketName();

        _minioClient = (MinioClient)new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();
    }


    public async Task<string> UploadImageFromHttpRequest(HttpListenerRequest request)
    {
        try
        {
            var contentType = request.ContentType?.ToLower();

            bool isValidContentType = !string.IsNullOrEmpty(contentType) &&
                (contentType.StartsWith("image/") ||
                 contentType.StartsWith("multipart/form-data") ||
                 contentType == "application/octet-stream");

            if (!isValidContentType)
            {
                throw new ArgumentException($"Invalid content type: {contentType}. Only images are allowed.");
            }

            if (request.ContentLength64 > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size too large. Maximum size is 5MB.");
            }

            string fileExtension = ".jpg"; // по умолчанию

            if (contentType.StartsWith("image/"))
            {
                fileExtension = GetFileExtension(contentType);
            }

            else if (contentType.StartsWith("multipart/form-data"))
            {
                fileExtension = GetFileExtensionFromHeaders(request);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            var bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));

            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                await SetBucketPolicy();
            }

            using var memoryStream = new MemoryStream();
            await request.InputStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var actualContentType = GetActualContentType(fileExtension);

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(uniqueFileName)
                .WithStreamData(memoryStream)
                .WithObjectSize(memoryStream.Length)
                .WithContentType(actualContentType));


            var directUrl = $"http://{ConfigHelper.GetMinIoEndpoint()}/{_bucketName}/{uniqueFileName}";

            return directUrl;
        }

        catch (Exception ex)
        {
            throw;
        }
    }


    public async Task<string> SaveBase64Image(string base64Data, string bucketName = "wishlister")
    {
        try
        {
            var parts = base64Data.Split(',');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid base64 data format");
            }

            var mimeType = parts[0].Split(';')[0].Split(':')[1];
            var imageBytes = Convert.FromBase64String(parts[1]);

            var fileExtension = mimeType.ToLower() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".jpg"
            };

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                await SetBucketPolicy();
            }

            using var memoryStream = new MemoryStream(imageBytes);
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(uniqueFileName)
                .WithStreamData(memoryStream)
                .WithObjectSize(memoryStream.Length)
                .WithContentType(mimeType));


            return $"http://{ConfigHelper.GetMinIoEndpoint()}/{bucketName}/{uniqueFileName}";
        }

        catch (Exception ex)
        {
            throw new Exception($"Failed to save image: {ex.Message}", ex);
        }
    }


    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            var objectName = GetObjectNameFromUrl(imageUrl);

            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName));

            return true;
        }

        catch (Exception ex)
        {
            return false;
        }
    }


    private string GetFileExtension(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            _ => ".jpg"
        };
    }


    private string GetFileExtensionFromHeaders(HttpListenerRequest request)
    {
        var contentDisposition = request.Headers["Content-Disposition"];
        if (!string.IsNullOrEmpty(contentDisposition))
        {
            var filenameMatch = System.Text.RegularExpressions.Regex.Match(
                contentDisposition, @"filename\*?=([^;]+)");

            if (filenameMatch.Success)
            {
                var filename = filenameMatch.Groups[1].Value.Trim('"');
                var extension = Path.GetExtension(filename).ToLower();

                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };
                if (allowedExtensions.Contains(extension))
                {
                    return extension;
                }
            }
        }

        return ".jpg";
    }


    private string GetActualContentType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
    }


    private async Task SetBucketPolicy()
    {
        try
        {
            var policy = $@"{{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {{
                        ""Effect"": ""Allow"",
                        ""Principal"": {{""AWS"": [""*""]}},
                        ""Action"": [""s3:GetObject""],
                        ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
                    }},
                    {{
                        ""Effect"": ""Allow"", 
                        ""Principal"": {{""AWS"": [""*""]}},
                        ""Action"": [""s3:GetBucketLocation""],
                        ""Resource"": [""arn:aws:s3:::{_bucketName}""]
                    }}
                ]
            }}";

            await _minioClient.SetPolicyAsync(new SetPolicyArgs()
                .WithBucket(_bucketName)
                .WithPolicy(policy));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MinIO] Error setting bucket policy: {ex.Message}");
        }
    }


    private string GetObjectNameFromUrl(string imageUrl)
    {
        var baseUrl = $"http://{ConfigHelper.GetMinIoEndpoint()}/{_bucketName}/";
        return imageUrl.Replace(baseUrl, "");
    }
}