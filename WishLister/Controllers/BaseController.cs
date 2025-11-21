using System.Net;
using System.Text;
using System.Text.Json;
using WishLister.Services;

namespace WishLister.Controllers;

public abstract class BaseController
{
    protected readonly SessionService _sessionService;

    protected BaseController(SessionService sessionService)
    {
        _sessionService = sessionService;
    }


    protected async Task<int?> GetAuthenticatedUserId(HttpListenerContext context)
    {
        var sessionId = ExtractSessionIdFromRequest(context.Request);

        if (string.IsNullOrEmpty(sessionId))
            return null;

        var session = await _sessionService.ValidateSessionAsync(sessionId);
        if (session == null)
            return null;

        return session.UserId;
    }


    protected string? ExtractSessionIdFromRequest(HttpListenerRequest request)
    {
        var authHeader = request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7);
        }

        var cookieHeader = request.Headers["Cookie"];
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            var cookies = cookieHeader.Split(';');
            foreach (var cookie in cookies)
            {
                var parts = cookie.Trim().Split('=', 2);
                if (parts.Length == 2 && parts[0].Trim() == "session_id")
                {
                    return parts[1].Trim();
                }
            }
        }

        var querySession = request.QueryString["session"];
        if (!string.IsNullOrEmpty(querySession))
        {
            return querySession;
        }

        return null;
    }


    protected static async Task<T> ReadRequestBody<T>(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ArgumentException("Invalid request body");
    }


    protected static async Task WriteJsonResponse(HttpListenerContext context, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }


    protected static int? GetIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var parts = url.Split('/');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var id))
                return id;
        }
        return null;
    }
}