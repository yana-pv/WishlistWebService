using System.Net;
using System.Text;
using System.Text.Json;
using WishLister.Models.Auth;
using WishLister.Services;

namespace WishLister.Controllers;
public class AuthController
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    public async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "";
            var method = request.HttpMethod;

            if (method == "POST" && path == "/api/auth/register")
            {
                await Register(context);
            }
            else if (method == "POST" && path == "/api/auth/login")
            {
                await Login(context);
            }
            else if (method == "POST" && path == "/api/auth/logout")
            {
                await Logout(context);
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


    private async Task Register(HttpListenerContext context)
    {
        var request = await ReadRequestBody<RegisterRequest>(context.Request);

        var result = await _authService.RegisterAsync(request);

        context.Response.ContentType = "application/json";

        if (result.success && !string.IsNullOrEmpty(result.sessionId))
        {
            context.Response.SetCookie(new Cookie("session_id", result.sessionId, "/")
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = false, 
            });

            await WriteJsonResponse(context, new
            {
                status = "success",
                message = result.message,
                user = new { id = result.sessionId }
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = result.message
            });
        }
    }


    private async Task Login(HttpListenerContext context)
    {
        var request = await ReadRequestBody<LoginRequest>(context.Request);

        var result = await _authService.LoginAsync(request);

        if (result.success && !string.IsNullOrEmpty(result.sessionId))
        {
            context.Response.SetCookie(new Cookie("session_id", result.sessionId, "/")
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = false, 
            });

            await WriteJsonResponse(context, new
            {
                status = "success",
                message = result.message
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = result.message
            });
        }
    }


    private async Task Logout(HttpListenerContext context)
    {
        var sessionId = ExtractSessionIdFromRequest(context.Request);
        if (!string.IsNullOrEmpty(sessionId))
        {
            await _authService.LogoutAsync(sessionId);
        }

        context.Response.SetCookie(new Cookie("session_id", "", "/")
        {
            Expires = DateTime.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
        });

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Выход выполнен"
        });
    }


    private string? ExtractSessionIdFromRequest(HttpListenerRequest request)
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


    private static async Task<T> ReadRequestBody<T>(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new ArgumentException("Invalid request body");
    }


    private static async Task WriteJsonResponse(HttpListenerContext context, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;

        await context.Response.OutputStream.WriteAsync(bytes);
    }
}