using System.Net;
using WishLister.Controllers;

namespace WishLister.Middleware;
public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthController _authController;
    private readonly UserController _userController;
    private readonly WishlistController _wishlistController;
    private readonly ItemController _itemController;
    private readonly FriendController _friendController;
    private readonly LinkController _linkController;
    private readonly ThemeController _themeController;

    public RoutingMiddleware(RequestDelegate next, AuthController authController,
                           UserController userController, WishlistController wishlistController,
                           ItemController itemController, FriendController friendController,
                           LinkController linkController, ThemeController themeController)
    {
        _next = next;
        _authController = authController;
        _userController = userController;
        _wishlistController = wishlistController;
        _itemController = itemController;
        _friendController = friendController;
        _linkController = linkController;
        _themeController = themeController;
    }

    public async Task InvokeAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.Url?.AbsolutePath?.StartsWith("/api/") == true)
        {
            await HandleApiRoutes(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleApiRoutes(HttpListenerContext context)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "";
        Console.WriteLine($"[Routing] Processing API route: {path}"); // <-- ДОБАВЬТЕ ЭТО

        try
        {
            // Публичные вишлисты
            if (path.StartsWith("/api/public/wishlists/"))
            {
                await _wishlistController.HandleRequest(context);
                return;
            }

            if (path == "/api/auth/check")
            {
                await _next(context); // Передаём в AuthMiddleware
                return;
            }

            // Остальные маршруты остаются без изменений
            if (path.StartsWith("/api/auth/"))
            {
                await _authController.HandleRequest(context);
                return;
            }

            

            if (path.StartsWith("/api/user/"))
            {
                Console.WriteLine($"[Routing] Routing to UserController: {path}"); // <-- ЛОГ
                await _userController.HandleRequest(context);
                return;
            }

            if (path.StartsWith("/api/wishlists"))
            {
                await _wishlistController.HandleRequest(context);
                return;
            }

            if (path.StartsWith("/api/themes"))
            {
                await _themeController.HandleRequest(context);
                return;
            }

            if (path.StartsWith("/api/items") || path == "/api/upload-image")
            {
                await _itemController.HandleRequest(context);
                return;
            }

            if (path.StartsWith("/api/links"))
            {
                await _linkController.HandleRequest(context);
                return;
            }

            if (path.StartsWith("/api/friend-wishlists"))
            {
                await _friendController.HandleRequest(context);
                return;
            }

            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "API route not found" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Routing] Error: {ex.Message}");
            context.Response.StatusCode = 500;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
    }

    private static async Task WriteJsonResponse(HttpListenerContext context, object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(bytes);
    }
}