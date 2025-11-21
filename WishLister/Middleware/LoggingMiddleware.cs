using System.Net;

namespace WishLister.Middleware;
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var startTime = DateTime.UtcNow;

        await _next(context);

        var duration = DateTime.UtcNow - startTime;
    }
}