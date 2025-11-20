using System.Net;
using WishLister.Repository.Implementations;
using WishLister.Repository.Interfaces;
using WishLister.Services;

namespace WishLister.Middleware;
public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SessionService _sessionService;
    private readonly IUserRepository _userRepository; // Добавляем отдельно


    public AuthMiddleware(RequestDelegate next, SessionService sessionService, IUserRepository userRepository)
    {
        _next = next;
        _sessionService = sessionService;
        _userRepository = userRepository;
    }

    // Упрощенный AuthMiddleware - только для /api/auth/check
    public async Task InvokeAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "";
        Console.WriteLine($"[AuthMiddleware] Path: {path}");

        // Специальный маршрут для проверки авторизации
        if (path == "/api/auth/check")
        {
            var sessionId = ExtractSessionIdFromRequest(context.Request);
            Console.WriteLine($"[AuthMiddleware] Session ID from request: {sessionId}");

            if (!string.IsNullOrEmpty(sessionId))
            {
                var session = await _sessionService.ValidateSessionAsync(sessionId);
                Console.WriteLine($"[AuthMiddleware] Session validation result: {session != null}");
                if (session != null)
                {
                    var user = await _userRepository.GetByIdAsync(session.UserId);
                    Console.WriteLine($"[AuthMiddleware] User found: {user != null}, ID: {user?.Id}");
                    if (user != null)
                    {
                        context.Response.Headers.Add("X-Is-Authenticated", "true");
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        var responseJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            authenticated = true,
                            userId = session.UserId,
                            username = user.Username
                        });
                        var bytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
                        await context.Response.OutputStream.WriteAsync(bytes);
                        return;
                    }
                }
            }

            context.Response.Headers.Add("X-Is-Authenticated", "false");
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var responseJson401 = System.Text.Json.JsonSerializer.Serialize(new { authenticated = false });
            var bytes401 = System.Text.Encoding.UTF8.GetBytes(responseJson401);
            await context.Response.OutputStream.WriteAsync(bytes401);
            return;
        }

        // Для всех остальных маршрутов просто пропускаем
        await _next(context);
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
}