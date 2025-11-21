using WishLister.Models; // Убираем: using WishLister.DTOs;
using WishLister.Repository.Implementations;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Services;
public class WishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IThemeRepository _themeRepository;
    private readonly ILinkRepository _linkRepository; 


    public WishlistService(IWishlistRepository wishlistRepository, IItemRepository itemRepository, IThemeRepository themeRepository, ILinkRepository linkRepository)
    {
        _wishlistRepository = wishlistRepository;
        _itemRepository = itemRepository;
        _themeRepository = themeRepository;
        _linkRepository = linkRepository;
    }


    public async Task<Wishlist?> CreateWishlistAsync(Wishlist wishlist, int userId)
    {
        var (isValid, validationMessage) = Validators.ValidateWishlist(wishlist.Title, wishlist.Description);
        if (!isValid)
        {
            throw new ArgumentException(validationMessage);
        }

        wishlist.UserId = userId;
        wishlist.ShareToken = Guid.NewGuid().ToString();

        return await _wishlistRepository.CreateAsync(wishlist);
    }


    public async Task<List<Wishlist>> GetUserWishlistsAsync(int userId)
    {
        var wishlists = await _wishlistRepository.GetByUserIdAsync(userId);

        foreach (var wishlist in wishlists)
        {
            wishlist.Theme = await _themeRepository.GetByIdAsync(wishlist.ThemeId);
        }

        return wishlists;
    }


    public async Task<Wishlist?> GetWishlistWithItemsAsync(int wishlistId, int? userId = null)
    {
        var wishlist = await _wishlistRepository.GetByIdAsync(wishlistId);
        if (wishlist == null) return null;

        wishlist.Theme = await _themeRepository.GetByIdAsync(wishlist.ThemeId);
        wishlist.Items = await _itemRepository.GetByWishlistIdAsync(wishlistId);
        return wishlist;
    }


    public async Task<Wishlist?> GetWishlistByShareTokenAsync(string shareToken)
    {
        var wishlist = await _wishlistRepository.GetByShareTokenAsync(shareToken);
        if (wishlist == null) return null;

        wishlist.Theme = await _themeRepository.GetByIdAsync(wishlist.ThemeId);
        wishlist.Items = await _itemRepository.GetByWishlistIdAsync(wishlist.Id);

        foreach (var item in wishlist.Items)
        {
            item.Links = await _linkRepository.GetByItemIdAsync(item.Id);
        }

        return wishlist;
    }


    public async Task<bool> CanUserEditWishlistAsync(int wishlistId, int userId)
    {
        return await _wishlistRepository.UserOwnsWishlistAsync(wishlistId, userId);
    }


    public async Task<Wishlist> UpdateWishlistAsync(Wishlist wishlist)
    {
        var (isValid, validationMessage) = Validators.ValidateWishlist(wishlist.Title, wishlist.Description);
        if (!isValid)
        {
            throw new ArgumentException(validationMessage);
        }

        return await _wishlistRepository.UpdateAsync(wishlist);
    }

    public async Task<bool> DeleteWishlistAsync(int id)
    {
        return await _wishlistRepository.DeleteAsync(id);
    }
}