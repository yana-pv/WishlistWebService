using System.Net;
using System.Text.Json;
using WishLister.Services;

namespace WishLister.Controllers;
public class ThemeController
{
    private readonly ThemeService _themeService;

    public ThemeController(ThemeService themeService)
    {
        _themeService = themeService;
    }


    public async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "";
            var method = request.HttpMethod;

            if (method == "GET" && path == "/api/themes")
            {
                await GetThemes(context);
            }
            else if (method == "GET" && path.StartsWith("/api/themes/"))
            {
                await GetTheme(context);
            }
            else
            {
                response.StatusCode = 404;
                await WriteJsonResponse(context, new { status = "error", message = "Route not found" });
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
    }


    private async Task GetThemes(HttpListenerContext context)
    {
        var themes = await _themeService.GetAllThemesAsync();

        await WriteJsonResponse(context, new
        {
            status = "success",
            themes = themes.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                color = t.Color,
                background = t.Background,
                buttonColor = t.ButtonColor
            })
        });
    }


    private async Task GetTheme(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var themeId = GetIdFromUrl(path);

        if (themeId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid theme ID" });
            return;
        }

        var theme = await _themeService.GetThemeAsync(themeId.Value);
        if (theme == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Theme not found" });
            return;
        }

        await WriteJsonResponse(context, new
        {
            status = "success",
            theme = new
            {
                id = theme.Id,
                name = theme.Name,
                color = theme.Color,
                background = theme.Background,
                buttonColor = theme.ButtonColor
            }
        });
    }


    private static int? GetIdFromUrl(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var parts = path.Split('/');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var id))
            {
                return id;
            }
        }
        return null;
    }


    private static async Task WriteJsonResponse(HttpListenerContext context, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }
}