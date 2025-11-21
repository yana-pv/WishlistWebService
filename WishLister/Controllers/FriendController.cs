using System.Net;
using System.Text.Json;
using WishLister.Models;
using WishLister.Services;

namespace WishLister.Controllers;
public class FriendController : BaseController
{
    private readonly FriendService _friendService;

    public FriendController(FriendService friendService, SessionService sessionService)
        : base(sessionService)
    {
        _friendService = friendService;
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

            if (request.HttpMethod == "GET" && path == "/api/friend-wishlists")
            {
                await GetFriendWishlists(context, userId.Value);
            }
            else if (request.HttpMethod == "POST" && path == "/api/friend-wishlists")
            {
                await AddFriendWishlist(context, userId.Value);
            }
            else if (request.HttpMethod == "GET" && path.StartsWith("/api/friend-wishlists/"))
            {
                await GetFriendWishlist(context, userId.Value);
            }
            else if (request.HttpMethod == "POST" && path == "/api/friend-wishlists/save-from-url")
            {
                await SaveFriendWishlistFromUrl(context, userId.Value);
            }
            else if (request.HttpMethod == "DELETE" && path.StartsWith("/api/friend-wishlists/"))
            {
                await DeleteFriendWishlist(context, userId.Value);
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


    private async Task GetFriendWishlists(HttpListenerContext context, int userId)
    {
        var friendWishlists = await _friendService.GetFriendWishlistsAsync(userId);

        await WriteJsonResponse(context, new
        {
            status = "success",
            wishlists = friendWishlists.Select(fw => new
            {
                id = fw.Id,
                wishlistId = fw.WishlistId,
                friendName = fw.FriendName,
                title = fw.Wishlist.Title,
                description = fw.Wishlist.Description,
                eventDate = fw.Wishlist.EventDate, 
                theme = new
                {
                    id = fw.Wishlist.Theme.Id,
                    name = fw.Wishlist.Theme.Name,
                    color = fw.Wishlist.Theme.Color
                },
                createdAt = fw.CreatedAt
            })
        });
    }


    private async Task AddFriendWishlist(HttpListenerContext context, int userId)
    {
        var request = await ReadRequestBody<AddFriendWishlistRequest>(context.Request);

        var friendWishlist = await _friendService.AddFriendWishlistAsync(
            userId, request.ShareToken, request.FriendName);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Вишлист друга добавлен",
            wishlist = new
            {
                id = friendWishlist.Id,
                wishlistId = friendWishlist.WishlistId,
                friendName = friendWishlist.FriendName,
                title = friendWishlist.Wishlist.Title
            }
        });
    }


    private async Task SaveFriendWishlistFromUrl(HttpListenerContext context, int userId)
    {
        var request = await ReadRequestBody<SaveFriendWishlistRequest>(context.Request);

        if (string.IsNullOrEmpty(request.ShareToken))
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Share token is required" });
            return;
        }

        try
        {
            var friendWishlist = await _friendService.SaveFriendWishlistFromUrlAsync(
                userId, request.ShareToken, request.FriendName);

            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Вишлист друга сохранен",
                friendWishlist = new
                {
                    id = friendWishlist.Id,
                    wishlistId = friendWishlist.WishlistId,
                    friendName = friendWishlist.FriendName
                }
            });
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
    }


    private async Task GetFriendWishlist(HttpListenerContext context, int userId)
    {
        var friendWishlistId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (friendWishlistId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid wishlist ID" });
            return;
        }

        var result = await _friendService.GetFriendWishlistWithItemsForDisplayAsync(friendWishlistId.Value, userId);

        if (result == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Wishlist not found" });
            return;
        }

        (FriendWishlist friendWishlist, Wishlist wishlist) = result.Value;

        var itemsForDisplay = wishlist.Items.Select(i => new
        {
            id = i.Id,
            title = i.Title,
            description = i.Description,
            price = i.Price,
            imageUrl = i.ImageUrl,
            desireLevel = i.DesireLevel,
            comment = i.Comment,
            isReserved = i.IsReserved,
            reservedByUserId = i.IsReserved ? i.ReservedByUserId : null,
            links = i.Links.Select(l => new
            {
                id = l.Id,
                url = l.Url,
                title = l.Title,
                price = l.Price,
                isFromAI = l.IsFromAI,
                isSelected = l.IsSelected
            })
        }).ToList();

        await WriteJsonResponse(context, new
        {
            status = "success",
            wishlist = new
            {
                id = wishlist.Id,
                title = wishlist.Title,
                description = wishlist.Description,
                eventDate = wishlist.EventDate,
                theme = new
                {
                    id = wishlist.Theme.Id,
                    name = wishlist.Theme.Name,
                    color = wishlist.Theme.Color,
                    background = wishlist.Theme.Background,
                    buttonColor = wishlist.Theme.ButtonColor
                },
                friendName = friendWishlist.FriendName,
                items = itemsForDisplay
            }
        });
    }


    private async Task DeleteFriendWishlist(HttpListenerContext context, int userId)
    {
        var friendWishlistId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (friendWishlistId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid friend wishlist ID" });
            return;
        }

        var success = await _friendService.DeleteFriendWishlistAsync(friendWishlistId.Value, userId);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Вишлист друга удален"
            });
        }
        else
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Friend wishlist not found" });
        }
    }
}


public class AddFriendWishlistRequest
{
    public string ShareToken { get; set; } = string.Empty;
    public string FriendName { get; set; } = string.Empty;
}


public class SaveFriendWishlistRequest
{
    public string ShareToken { get; set; } = string.Empty;
    public string? FriendName { get; set; }
}