using Microsoft.Extensions.Configuration;

namespace WishLister.Utils;
public static class ConfigHelper
{
    private static readonly IConfigurationRoot Configuration;

    static ConfigHelper()
    {
        try
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load appsettings.json: {ex.Message}");
            Configuration = new ConfigurationBuilder().Build();
        }
    }

    public static string GetConnectionString() =>
        Configuration.GetConnectionString("DefaultConnection") ??
        "Host=localhost;Port=5432;Database=wishlist_db;Username=postgres;Password=postgresyanapv;";

    public static string GetJwtKey() => Configuration["Jwt:Key"] ?? "MySuperSecretKeyForWishListerApp2025!";
    public static string GetJwtIssuer() => Configuration["Jwt:Issuer"] ?? "WishLister";
    public static string GetJwtAudience() => Configuration["Jwt:Audience"] ?? "WishListerUsers";

    public static string GetMinIoEndpoint() => Configuration["MinIO:Endpoint"] ?? "127.0.0.1:9000";
    public static string GetMinIoAccessKey() => Configuration["MinIO:AccessKey"] ?? "admin";
    public static string GetMinIoSecretKey() => Configuration["MinIO:SecretKey"] ?? "password";
    public static string GetMinIoBucketName() => Configuration["MinIO:BucketName"] ?? "wishlister";

    // Уберем S3 настройки или оставим для совместимости
    public static string GetS3AccessKey() => GetMinIoAccessKey();
    public static string GetS3SecretKey() => GetMinIoSecretKey();
    public static string GetS3BucketName() => GetMinIoBucketName();
    public static string GetS3Region() => "us-east-1"; // Для MinIO не важно
    public static string GetS3ServiceUrl() => $"http://{GetMinIoEndpoint()}";
    public static string[] GetAllowedOrigins()
    {
        var originsString = Configuration["AllowedOrigins"] ?? "http://localhost:5000";
        return originsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(x => x.Trim())
                           .ToArray();
    }
}