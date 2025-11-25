using System.Net;

namespace WishLister.Middleware;
public class StaticFilesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _staticFilesPath;

    public StaticFilesMiddleware(RequestDelegate next)
    {
        _next = next;
        _staticFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "frontend");
    }

    public async Task InvokeAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? ""; 

        if (path.StartsWith("/wishlist/"))
        {
            var publicWishlistPath = Path.Combine(_staticFilesPath, "public-wishlist.html");
            if (File.Exists(publicWishlistPath))
            {
                await ServeStaticFile(context, publicWishlistPath);
                return;
            }
        }

        if (path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        var filePath = GetFilePath(request.Url?.LocalPath ?? "/");

        if (File.Exists(filePath))
        {
            await ServeStaticFile(context, filePath);
        }

        else
        {
            var indexPath = Path.Combine(_staticFilesPath, "index.html");
            if (File.Exists(indexPath))
            {
                await ServeStaticFile(context, indexPath);
            }
            else
            {
                response.StatusCode = 404;
                await _next(context);
            }
        }
    }

    private string GetFilePath(string path)
    {
        if (path == "/") path = "/index.html";
        var filePath = Path.Combine(_staticFilesPath, path.TrimStart('/'));
        if (Directory.Exists(filePath))
        {
            filePath = Path.Combine(filePath, "index.html");
        }
        return filePath;
    }

    private async Task ServeStaticFile(HttpListenerContext context, string filePath)
    {
        var response = context.Response;

        try
        {
            var content = await File.ReadAllBytesAsync(filePath);
            response.ContentType = GetContentType(filePath);
            response.StatusCode = 200;

            await response.OutputStream.WriteAsync(content);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await _next(context);
        }
    }

    private string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}