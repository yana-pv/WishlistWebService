using System.Net;
using System.Text.Json;
using WishLister.Models;
using WishLister.Repository.Implementations;
using WishLister.Repository.Interfaces;
using WishLister.Services;

namespace WishLister.Controllers;
public class WishlistController : BaseController
{
    private readonly WishlistService _wishlistService;
    private readonly ThemeService _themeService;
    private readonly IItemRepository _itemRepository; 


    public WishlistController(WishlistService wishlistService, ThemeService themeService, IItemRepository itemRepository, SessionService sessionService)
        : base(sessionService)
    {
        _wishlistService = wishlistService;
        _themeService = themeService;
        _itemRepository = itemRepository; 

    }

    public async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "";
            var method = request.HttpMethod;

            // Публичный доступ к вишлисту по share token
            if (method == "GET" && path.StartsWith("/api/public/wishlists/"))
            {
                var publicUserId = await GetAuthenticatedUserId(context); // переименовали переменную
                await GetPublicWishlist(context, publicUserId);
                return;
            }

            // Для защищенных маршрутов проверяем аутентификацию
            var authenticatedUserId = await GetAuthenticatedUserId(context); // переименовали переменную
            Console.WriteLine($"[WishlistController] Authenticated user ID: {authenticatedUserId}");

            if (method == "GET" && path == "/api/wishlists")
            {
                if (authenticatedUserId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await GetUserWishlists(context, authenticatedUserId.Value);
            }
            else if (method == "POST" && path == "/api/wishlists")
            {
                if (authenticatedUserId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await CreateWishlist(context, authenticatedUserId.Value);
            }
            else if (method == "GET" && path.StartsWith("/api/wishlists/"))
            {
                await GetWishlist(context, authenticatedUserId);
            }
            else if (method == "PUT" && path.StartsWith("/api/wishlists/"))
            {
                if (authenticatedUserId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await UpdateWishlist(context, authenticatedUserId.Value);
            }
            // В методе HandleRequest, после UpdateWishlist, добавь:
            else if (method == "DELETE" && path.StartsWith("/api/wishlists/"))
            {
                if (authenticatedUserId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await DeleteWishlist(context, authenticatedUserId.Value);
            }

            else if (method == "GET" && path == "/api/themes")
            {
                await GetThemes(context);
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

    private async Task GetPublicWishlist(HttpListenerContext context, int? userId)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var shareToken = path.Replace("/api/public/wishlists/", "");

        if (string.IsNullOrEmpty(shareToken))
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Share token is required" });
            return;
        }

        var wishlist = await _wishlistService.GetWishlistByShareTokenAsync(shareToken);
        if (wishlist == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Wishlist not found" });
            return;
        }

        // Для публичного доступа скрываем информацию о том, кто забронировал подарки
        // Для авторизованных пользователей показываем кто забронировал
        var showReservationDetails = userId.HasValue;

        // Подготавливаем список предметов с нужной информацией
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
            reservedByUserId = showReservationDetails ? i.ReservedByUserId : null,
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

        var isOwner = userId.HasValue && wishlist.UserId == userId.Value;

        await WriteJsonResponse(context, new
        {
            status = "success",
            wishlist = new
            {
                id = wishlist.Id,
                title = wishlist.Title,
                description = wishlist.Description,
                eventDate = wishlist.EventDate?.ToString("yyyy-MM-dd"), // Изменено
                theme = new
                {
                    id = wishlist.Theme.Id,
                    name = wishlist.Theme.Name,
                    color = wishlist.Theme.Color,
                    background = wishlist.Theme.Background,
                    buttonColor = wishlist.Theme.ButtonColor
                },
                isOwner = isOwner,
                items = itemsForDisplay
            }
        });
    }

    private async Task GetUserWishlists(HttpListenerContext context, int userId)
    {
        var wishlists = await _wishlistService.GetUserWishlistsAsync(userId);

        var wishlistData = new List<object>();

        foreach (var w in wishlists)
        {
            // Подсчитываем количество подарков для каждого вишлиста
            var itemCount = await _itemRepository.GetItemsCountByWishlistAsync(w.Id);

            wishlistData.Add(new
            {
                id = w.Id,
                title = w.Title,
                description = w.Description,
                eventDate = w.EventDate?.ToString("yyyy-MM-dd"),
                theme = new
                {
                    id = w.Theme.Id,
                    name = w.Theme.Name,
                    color = w.Theme.Color
                },
                shareToken = w.ShareToken,
                itemCount = itemCount, // <-- Используем реальное количество
                createdAt = w.CreatedAt
            });
        }

        await WriteJsonResponse(context, new
        {
            status = "success",
            wishlists = wishlistData
        });
    }

    private async Task CreateWishlist(HttpListenerContext context, int userId)
    {
        var request = await ReadRequestBody<CreateWishlistRequest>(context.Request);

        var wishlist = new Wishlist
        {
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            ThemeId = request.ThemeId
        };

        var createdWishlist = await _wishlistService.CreateWishlistAsync(wishlist, userId);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Вишлист создан",
            wishlist = new
            {
                id = createdWishlist.Id,
                title = createdWishlist.Title,
                shareToken = createdWishlist.ShareToken
            }
        });
    }

    private async Task GetWishlist(HttpListenerContext context, int? userId)
    {
        var wishlistId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (wishlistId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid wishlist ID" });
            return;
        }

        var wishlist = await _wishlistService.GetWishlistWithItemsAsync(wishlistId.Value, userId);

        if (wishlist == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Wishlist not found" });
            return;
        }

        var isOwner = userId.HasValue && wishlist.UserId == userId.Value;

        // Подготавливаем список предметов с нужной информацией
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
            reservedByUserId = isOwner ? null : i.ReservedByUserId,
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
                eventDate = wishlist.EventDate?.ToString("yyyy-MM-dd"), // Изменено
                theme = new
                {
                    id = wishlist.Theme.Id,
                    name = wishlist.Theme.Name,
                    color = wishlist.Theme.Color,
                    background = wishlist.Theme.Background,
                    buttonColor = wishlist.Theme.ButtonColor
                },
                isOwner = isOwner,
                shareToken = wishlist.ShareToken,
                items = itemsForDisplay
            }
        });
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

    private async Task UpdateWishlist(HttpListenerContext context, int userId)
    {
        var wishlistId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (wishlistId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid wishlist ID" });
            return;
        }

        if (!await _wishlistService.CanUserEditWishlistAsync(wishlistId.Value, userId))
        {
            context.Response.StatusCode = 403;
            await WriteJsonResponse(context, new { status = "error", message = "Access denied" });
            return;
        }

        var request = await ReadRequestBody<UpdateWishlistRequest>(context.Request);

        var wishlist = new Wishlist
        {
            Id = wishlistId.Value,
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            ThemeId = request.ThemeId
        };

        var updatedWishlist = await _wishlistService.UpdateWishlistAsync(wishlist);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Вишлист обновлен",
            wishlist = new
            {
                id = updatedWishlist.Id,
                title = updatedWishlist.Title,
                eventDate = updatedWishlist.EventDate?.ToString("yyyy-MM-dd"), // Добавлено
                description = updatedWishlist.Description,
                themeId = updatedWishlist.ThemeId
            }
        });
    }

    private async Task DeleteWishlist(HttpListenerContext context, int userId)
    {
        var wishlistId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (wishlistId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid wishlist ID" });
            return;
        }

        if (!await _wishlistService.CanUserEditWishlistAsync(wishlistId.Value, userId))
        {
            context.Response.StatusCode = 403;
            await WriteJsonResponse(context, new { status = "error", message = "Access denied" });
            return;
        }

        var success = await _wishlistService.DeleteWishlistAsync(wishlistId.Value);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Вишлист удален"
            });
        }
        else
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Wishlist not found" });
        }
    }
}

public class CreateWishlistRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public int ThemeId { get; set; }
}

public class UpdateWishlistRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public int ThemeId { get; set; }
}