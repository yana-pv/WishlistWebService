using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Services;
public class LinkService
{
    private readonly ILinkRepository _linkRepository;
    private readonly HybridProductSearchService _searchService;


    public LinkService(ILinkRepository linkRepository, HybridProductSearchService searchService)
    {
        _linkRepository = linkRepository;
        _searchService = searchService;
    }


    public async Task<ItemLink> AddLinkAsync(ItemLink link)
    {
        if (string.IsNullOrWhiteSpace(link.Url))
        {
            throw new ArgumentException("URL ссылки обязателен");
        }

        if (!Validators.IsValidUrl(link.Url))
        {
            throw new ArgumentException("Некорректный URL");
        }

        return await _linkRepository.CreateAsync(link);
    }


    public async Task<bool> DeleteLinkAsync(int linkId)
    {
        return await _linkRepository.DeleteAsync(linkId);
    }


    public async Task<List<ItemLink>> GenerateAILinksAsync(string itemTitle)
    {
        var searchResults = await _searchService.SearchProductsAsync(itemTitle);

        return searchResults.Select(result => new ItemLink
        {
            Url = result.Url,
            Title = result.Title,
            IsFromAI = true,
        }).ToList();
    }
}