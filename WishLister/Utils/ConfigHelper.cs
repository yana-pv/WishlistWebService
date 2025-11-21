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
        Configuration.GetConnectionString("DefaultConnection");

    public static string GetMinIoEndpoint() => Configuration["MinIO:Endpoint"];
    public static string GetMinIoAccessKey() => Configuration["MinIO:AccessKey"];
    public static string GetMinIoSecretKey() => Configuration["MinIO:SecretKey"];
    public static string GetMinIoBucketName() => Configuration["MinIO:BucketName"];
}