using System.Net;
using System.Text.Json;
using WishLister.Models;
using WishLister.Services;

namespace WishLister.Controllers;
public class UserController : BaseController
{
    private readonly UserService _userService;

    public UserController(UserService userService, SessionService sessionService)
        : base(sessionService)
    {
        _userService = userService;
    }


    public async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var userId = await GetAuthenticatedUserId(context);

            if (userId == null)
            {
                response.StatusCode = 401;
                await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                return;
            }

            var path = request.Url?.AbsolutePath ?? "";

            if (request.HttpMethod == "GET" && path == "/api/user/profile")
            {
                await GetProfile(context, userId.Value);
            }
            else if (request.HttpMethod == "PUT" && path == "/api/user/profile")
            {
                await UpdateProfile(context, userId.Value);
            }
            else if (request.HttpMethod == "GET" && path == "/api/user/stats")
            {
                await GetStats(context, userId.Value);
            }
            else if (request.HttpMethod == "DELETE" && path == "/api/user/profile")
            {
                await DeleteAccount(context, userId.Value);
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


    private async Task GetProfile(HttpListenerContext context, int userId)
    {
        var user = await _userService.GetUserProfileAsync(userId);
        if (user == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "User not found" });
            return;
        }

        await WriteJsonResponse(context, new
        {
            status = "success",
            user = new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                avatarUrl = user.AvatarUrl, 
                createdAt = user.CreatedAt
            }
        });
    }


    private async Task UpdateProfile(HttpListenerContext context, int userId)
    {
        var request = await ReadRequestBody<UpdateProfileRequest>(context.Request);

        var user = await _userService.UpdateUserProfileAsync(
            userId, request.Username, request.Email, request.AvatarUrl);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Профиль обновлен",
            user = new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                avatarUrl = user.AvatarUrl
            }
        });
    }


    private async Task DeleteAccount(HttpListenerContext context, int userId)
    {
        var request = await ReadRequestBody<DeleteAccountRequest>(context.Request);

        await _userService.DeleteAccountAsync(userId, request.ConfirmPassword);

        context.Response.SetCookie(new Cookie("session_id", "", "/")
        {
            Expires = DateTime.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = false,
        });

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Аккаунт успешно удалён"
        });
    }


    private async Task GetStats(HttpListenerContext context, int userId)
    {
        var stats = await _userService.GetUserStatsAsync(userId);

        await WriteJsonResponse(context, new
        {
            status = "success",
            stats
        });
    }
}


public class UpdateProfileRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}


public class DeleteAccountRequest
{
    public string ConfirmPassword { get; set; } = string.Empty;
}


