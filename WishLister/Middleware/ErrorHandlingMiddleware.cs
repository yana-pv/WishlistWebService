using System.Net;
using System.Text.Json;

namespace WishLister.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpListenerContext context)
    {
        try
        {
            await _next(context);
        }

        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }


    private static async Task HandleExceptionAsync(HttpListenerContext context, Exception exception)
    {
        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            _ => 500
        };

        var errorResponse = new
        {
            status = "error",
            message = exception.Message,
            details = context.Response.StatusCode == 500 ? "Internal server error" : exception.Message
        };

        var json = JsonSerializer.Serialize(errorResponse);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(bytes);
    }
}