using System.Net;
using WishLister.Models.Entities;
using WishLister.Models.Requests.Links;
using WishLister.Services;

namespace WishLister.Controllers;
public class LinkController : BaseController
{
    private readonly LinkService _linkService;

    public LinkController(LinkService linkService, SessionService sessionService)
        : base(sessionService)
    {
        _linkService = linkService;
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

            if (method == "GET" && path.StartsWith("/api/links/ai/"))
            {
                await GenerateAILinks(context);
            }
            else if (method == "POST" && path == "/api/links")
            {
                if (userId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await AddLink(context);
            }
            else if (method == "PUT" && path.StartsWith("/api/links/"))
            {
                if (userId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await UpdateLink(context);
            }
            else if (method == "DELETE" && path.StartsWith("/api/links/"))
            {
                if (userId == null)
                {
                    response.StatusCode = 401;
                    await WriteJsonResponse(context, new { status = "error", message = "Unauthorized" });
                    return;
                }
                await DeleteLink(context);
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


    private async Task GenerateAILinks(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var itemTitle = path.Replace("/api/links/ai/", "");

        if (string.IsNullOrEmpty(itemTitle))
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Item title is required" });
            return;
        }

        var decodedTitle = Uri.UnescapeDataString(itemTitle);
        var links = await _linkService.GenerateAILinksAsync(decodedTitle);

        await WriteJsonResponse(context, new
        {
            status = "success",
            links = links.Select(l => new
            {
                url = l.Url,
                title = l.Title,
                isFromAI = l.IsFromAI,
                isSelected = l.IsSelected
            })
        });
    }


    private async Task AddLink(HttpListenerContext context)
    {
        var request = await ReadRequestBody<AddLinkRequest>(context.Request);

        var link = new ItemLink
        {
            Url = request.Url,
            Title = request.Title,
            Price = request.Price,
            IsFromAI = request.IsFromAI,
            IsSelected = request.IsSelected,
            ItemId = request.ItemId
        };

        var createdLink = await _linkService.AddLinkAsync(link);

        await WriteJsonResponse(context, new
        {
            status = "success",
            message = "Ссылка добавлена",
            link = new
            {
                id = createdLink.Id,
                url = createdLink.Url,
                title = createdLink.Title,
                price = createdLink.Price,
                isFromAI = createdLink.IsFromAI,
                isSelected = createdLink.IsSelected
            }
        });
    }


    private async Task UpdateLink(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var linkId = GetIdFromUrl(path);
        if (linkId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid link ID" });
            return;
        }

        var request = await ReadRequestBody<UpdateLinkRequest>(context.Request);

        if (request.IsSelected)
        {
            var success = await _linkService.SetSelectedLinkAsync(request.ItemId, linkId.Value);
            if (success)
            {
                await WriteJsonResponse(context, new
                {
                    status = "success",
                    message = "Ссылка выбрана как основная"
                });
            }
            else
            {
                await WriteJsonResponse(context, new
                {
                    status = "error",
                    message = "Не удалось выбрать ссылку"
                });
            }
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Ссылка обновлена"
            });
        }
    }


    private async Task DeleteLink(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "";
        var linkId = GetIdFromUrl(path);
        if (linkId == null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonResponse(context, new { status = "error", message = "Invalid link ID" });
            return;
        }

        var success = await _linkService.DeleteLinkAsync(linkId.Value);

        if (success)
        {
            await WriteJsonResponse(context, new
            {
                status = "success",
                message = "Ссылка удалена"
            });
        }
        else
        {
            await WriteJsonResponse(context, new
            {
                status = "error",
                message = "Не удалось удалить ссылку"
            });
        }
    }
}





