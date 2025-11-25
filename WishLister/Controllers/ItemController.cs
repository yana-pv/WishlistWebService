using System.Net;
using System.Text.Json;
using WishLister.Models.Entities;
using WishLister.Models.Requests.Items;
using WishLister.Services;

namespace WishLister.Controllers;
public class ItemController : BaseController
{
    private readonly ItemService _itemService;
    private readonly MinIOService _minioService;

    public ItemController(ItemService itemService, MinIOService minioService, SessionService sessionService)
        : base(sessionService)
    {
        _itemService = itemService;
        _minioService = minioService;
    }


    public async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var userId = await GetAuthenticatedUserId(context);
            var path = request.Url?.AbsolutePath ?? "";
            var method = request.HttpMethod;

            if (RequiresAuth(path, method) && userId == null)
            {
                response.StatusCode = 401;
                await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                return;
            }

            if (method == "POST" && path == "/api/items")
            {
                await CreateItem(context, userId!.Value);
            }
            else if (method == "GET" && path.StartsWith("/api/items/"))
            {
                if (userId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await GetItem(context, userId.Value);
            }
            else if (method == "PUT" && path.StartsWith("/api/items/"))
            {
                await UpdateItem(context, userId!.Value);
            }
            else if (method == "DELETE" && path.StartsWith("/api/items/"))
            {
                await DeleteItem(context, userId!.Value);
            }
            else if (method == "POST" && path.Contains("/reserve"))
            {
                await ReserveItem(context, userId!.Value);
            }
            else if (method == "POST" && path.Contains("/unreserve"))
            {
                await UnreserveItem(context, userId!.Value);
            }
            else if (method == "POST" && path == "/api/upload-image")
            {
                await UploadImage(context);
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


    private bool RequiresAuth(string path, string method)
    {
        if (method == "GET")
        {
            return false; 
        }
        return true;
    }


    private async Task CreateItem(HttpListenerContext context, int userId)
    {
        try
        {
            var requestWithImage = await ReadRequestBody<CreateItemRequestWithImage>(context.Request);
            await CreateItemWithImage(context, userId, requestWithImage);
            return;
        }
        catch (JsonException)
        {
            var request = await ReadRequestBody<CreateItemRequest>(context.Request);
            await CreateItemWithUrl(context, userId, request);
        }
    }


    private async Task CreateItemWithImage(HttpListenerContext context, int userId, CreateItemRequestWithImage request)
    {
        string? imageUrl = null;

        if (!string.IsNullOrEmpty(request.ImageData))
        {
            try
            {
                imageUrl = await _minioService.SaveBase64Image(request.ImageData);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await WriteJsonResponse(context, new
                {
                    status = "error",
                    message = $"Ошибка загрузки изображения: {ex.Message}"
                });
                return;
            }
        }

        var item = new WishlistItem
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = imageUrl, 
            DesireLevel = request.DesireLevel,
            Comment = request.Comment,
            WishlistId = request.WishlistId
        };

        var links = request.Links?.Select(l => new ItemLink
        {
            Url = l.Url,
            Title = l.Title,
            IsFromAI = l.IsFromAI,
        }).ToList() ?? new List<ItemLink>();

        try
        {
            var createdItem = await _itemService.CreateItemAsync(item, links);

            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Товар добавлен в вишлист",
                item = new
                {
                    id = createdItem.Id,
                    title = createdItem.Title,
                    description = createdItem.Description,
                    price = createdItem.Price,
                    imageUrl = createdItem.ImageUrl,
                    desireLevel = createdItem.DesireLevel,
                    comment = createdItem.Comment,
                    links = createdItem.Links.Select(l => new
                    {
                        id = l.Id,
                        url = l.Url,
                        title = l.Title,
                        isFromAI = l.IsFromAI,
                    })
                }
            });
        }

        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                await _minioService.DeleteImageAsync(imageUrl);   
            }

            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = $"Ошибка создания товара: {ex.Message}"
            });
        }
    }


    private async Task CreateItemWithUrl(HttpListenerContext context, int userId, CreateItemRequest request)
    {
        var item = new WishlistItem
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            DesireLevel = request.DesireLevel,
            Comment = request.Comment,
            WishlistId = request.WishlistId
        };

        var links = request.Links?.Select(l => new ItemLink
        {
            Url = l.Url,
            Title = l.Title,
            IsFromAI = l.IsFromAI,
        }).ToList() ?? new List<ItemLink>();

        var createdItem = await _itemService.CreateItemAsync(item, links);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Товар добавлен в вишлист",
            item = new
            {
                id = createdItem.Id,
                title = createdItem.Title,
                description = createdItem.Description,
                price = createdItem.Price,
                imageUrl = createdItem.ImageUrl,
                desireLevel = createdItem.DesireLevel,
                comment = createdItem.Comment,
                links = createdItem.Links.Select(l => new
                {
                    id = l.Id,
                    url = l.Url,
                    title = l.Title,
                    isFromAI = l.IsFromAI,
                })
            }
        });
    }


    private async Task GetItem(HttpListenerContext context, int userId)
    {
        var itemId = GetIdFromUrl(context.Request.Url?.AbsolutePath);
        if (itemId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid item ID" });
            return;
        }

        var item = await _itemService.GetItemByIdAsync(itemId.Value, userId);

        if (item == null)
        {
            context.Response.StatusCode = 404;
            await WriteJsonResponse(context, new { status = "error", message = "Item not found" });
            return;
        }

        await WriteJsonResponse(context, new
        {
            status = "success",
            item = new
            {
                id = item.Id,
                title = item.Title,
                description = item.Description,
                price = item.Price,
                imageUrl = item.ImageUrl,
                desireLevel = item.DesireLevel,
                comment = item.Comment,
                isReserved = item.IsReserved,
                reservedByUserId = item.IsReserved ? item.ReservedByUserId : null,
                links = item.Links.Select(l => new
                {
                    id = l.Id,
                    url = l.Url,
                    title = l.Title,
                    isFromAI = l.IsFromAI,
                }),
                wishlistId = item.WishlistId 
            }
        });
    }


    private async Task UpdateItem(HttpListenerContext context, int userId)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var itemId = GetIdFromUrl(path);
        if (itemId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid item ID" });
            return;
        }

        if (!await _itemService.CanUserEditItemAsync(itemId.Value, userId))
        {
            context.Response.StatusCode = 403;
            await WriteJsonResponse(context, new { status = "error", message = "Access denied" });
            return;
        }

        var request = await ReadRequestBody<UpdateItemRequest>(context.Request);

        string imageUrlToUpdate = request.ImageUrl; 

        if (string.IsNullOrEmpty(request.ImageData))
        {
            var existingItem = await _itemService.GetItemByIdAsync(itemId.Value, userId);
            if (existingItem != null)
            {
                imageUrlToUpdate = existingItem.ImageUrl; 
            }
        }
        else
        {
            imageUrlToUpdate = await _minioService.SaveBase64Image(request.ImageData);
        }

        var item = new WishlistItem
        {
            Id = itemId.Value,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = imageUrlToUpdate,
            DesireLevel = request.DesireLevel,
            Comment = request.Comment
        };

        try
        {
            var updatedItem = await _itemService.UpdateItemAsync(item, request.Links);

            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Товар обновлен",
                item = new
                {
                    id = updatedItem.Id,
                    title = updatedItem.Title,
                    description = updatedItem.Description,
                    price = updatedItem.Price,
                    imageUrl = updatedItem.ImageUrl, 
                    desireLevel = updatedItem.DesireLevel,
                    comment = updatedItem.Comment,
                    links = updatedItem.Links.Select(l => new
                    {
                        id = l.Id,
                        url = l.Url,
                        title = l.Title,
                        isFromAI = l.IsFromAI,
                    })
                }
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
    }


    private async Task DeleteItem(HttpListenerContext context, int userId)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var itemId = GetIdFromUrl(path);
        if (itemId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid item ID" });
            return;
        }

        if (!await _itemService.CanUserEditItemAsync(itemId.Value, userId))
        {
            context.Response.StatusCode = 403;
            await WriteJsonResponse(context, new { status = "error", message = "Access denied" });
            return;
        }

        var success = await _itemService.DeleteItemAsync(itemId.Value);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Товар удален"
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = "Не удалось удалить товар"
            });
        }
    }


    private async Task ReserveItem(HttpListenerContext context, int userId)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var itemId = GetIdFromUrl(path.Replace("/reserve", ""));
        if (itemId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid item ID" });
            return;
        }

        var success = await _itemService.ReserveItemAsync(itemId.Value, userId);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Подарок забронирован"
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = "Не удалось забронировать подарок"
            });
        }
    }


    private async Task UnreserveItem(HttpListenerContext context, int userId)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var itemId = GetIdFromUrl(path.Replace("/unreserve", ""));
        if (itemId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid item ID" });
            return;
        }

        var success = await _itemService.UnreserveItemAsync(itemId.Value, userId);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Бронирование отменено"
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = "Не удалось отменить бронирование"
            });
        }
    }


    private async Task UploadImage(HttpListenerContext context)
    {
        var request = context.Request;

        if (!request.HasEntityBody)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "No file uploaded" });
            return;
        }

        try
        {
            var imageUrl = await _minioService.UploadImageFromHttpRequest(request);

            await WriteJsonResponse(context, new
            {
                status = "success",
                imageUrl
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = ex.Message });
        }
    }
}











