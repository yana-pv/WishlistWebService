using WishLister.Controllers;
using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Services;
public class ItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly ILinkRepository _linkRepository;
    private readonly IWishlistRepository _wishlistRepository;

    public ItemService(IItemRepository itemRepository, ILinkRepository linkRepository, IWishlistRepository wishlistRepository)
    {
        _itemRepository = itemRepository;
        _linkRepository = linkRepository;
        _wishlistRepository = wishlistRepository;
    }


    public async Task<WishlistItem> CreateItemAsync(WishlistItem item, List<ItemLink> links)
    {
        var (isValid, validationMessage) = Validators.ValidateItem(item.Title, item.Price, item.DesireLevel);
        if (!isValid)
        {
            throw new ArgumentException(validationMessage);
        }

        var wishlist = await _wishlistRepository.GetByIdAsync(item.WishlistId);
        if (wishlist == null)
            throw new KeyNotFoundException("Вишлист не найден");

        var createdItem = await _itemRepository.CreateAsync(item);

        List<ItemLink> createdLinks = new List<ItemLink>();
        foreach (var link in links)
        {
            link.ItemId = createdItem.Id;
            var createdLink = await _linkRepository.CreateAsync(link);
            createdLinks.Add(createdLink);
        }

        if (createdLinks.Any())
        {
            ItemLink selectedLink = null;
            selectedLink = createdLinks.FirstOrDefault(l => l.IsFromAI && l.IsSelected) ??
                           createdLinks.FirstOrDefault(l => l.IsFromAI) ??
                           createdLinks.FirstOrDefault(l => !l.IsFromAI && l.IsSelected) ??
                           createdLinks.FirstOrDefault(l => !l.IsFromAI) ??
                           createdLinks.First();
            await _linkRepository.SetSelectedLinkAsync(createdItem.Id, selectedLink.Id);
        }

        return createdItem;
    }


    public async Task<WishlistItem?> GetItemByIdAsync(int itemId, int userId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);

        if (item != null)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(item.WishlistId);

            if (wishlist != null && wishlist.UserId == userId)
            {
                item.Links = await _linkRepository.GetByItemIdAsync(itemId);
                return item;
            }
        }

        return null;
    }


    public async Task<bool> ReserveItemAsync(int itemId, int userId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null)
            throw new KeyNotFoundException("Товар не найден");

        if (item.IsReserved)
            throw new InvalidOperationException("Товар уже забронирован");

        return await _itemRepository.ReserveItemAsync(itemId, userId);
    }


    public async Task<bool> UnreserveItemAsync(int itemId, int userId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null)
            throw new KeyNotFoundException("Товар не найден");

        if (!item.IsReserved || item.ReservedByUserId != userId)
            throw new UnauthorizedAccessException("Вы не можете освободить этот товар");

        return await _itemRepository.UnreserveItemAsync(itemId, userId);
    }


    public async Task<WishlistItem> UpdateItemAsync(WishlistItem item, List<Models.Requests.Links.CreateLinkRequest>? links = null)
    {
        var (isValid, validationMessage) = Validators.ValidateItem(item.Title, item.Price, item.DesireLevel);
        if (!isValid)
        {
            throw new ArgumentException(validationMessage);
        }

        var updatedItem = await _itemRepository.UpdateAsync(item);

        if (links != null)
        {
            await _linkRepository.DeleteByItemIdAsync(updatedItem.Id);

            foreach (var link in links)
            {
                var newLink = new ItemLink
                {
                    Url = link.Url,
                    Title = link.Title,
                    Price = link.Price,
                    IsFromAI = link.IsFromAI,
                    IsSelected = link.IsSelected,
                    ItemId = updatedItem.Id
                };

                await _linkRepository.CreateAsync(newLink);
            }

            updatedItem.Links = await _linkRepository.GetByItemIdAsync(updatedItem.Id);
        }

        return updatedItem;
    }


    public async Task<bool> CanUserEditItemAsync(int itemId, int userId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null) return false;

        return await _wishlistRepository.UserOwnsWishlistAsync(item.WishlistId, userId);
    }


    public async Task<bool> DeleteItemAsync(int itemId)
    {
        var links = await _linkRepository.GetByItemIdAsync(itemId);
        foreach (var link in links)
        {
            await _linkRepository.DeleteAsync(link.Id);
        }

        return await _itemRepository.DeleteAsync(itemId);
    }
}