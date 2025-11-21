using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;

namespace WishLister.Services;
public class FriendService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IThemeRepository _themeRepository;
    private readonly IItemRepository _itemRepository;

    public FriendService(IFriendRepository friendRepository, IWishlistRepository wishlistRepository,
                        IThemeRepository themeRepository, IItemRepository itemRepository)
    {
        _friendRepository = friendRepository;
        _wishlistRepository = wishlistRepository;
        _themeRepository = themeRepository;
        _itemRepository = itemRepository;
    }


    public async Task<List<FriendWishlist>> GetFriendWishlistsAsync(int userId)
    {
        var friendWishlists = await _friendRepository.GetByUserIdAsyncWithWishlist(userId);

        foreach (var fw in friendWishlists)
        {
            if (fw.Wishlist != null && fw.Wishlist.ThemeId != 0)
            {
                fw.Wishlist.Theme = await _themeRepository.GetByIdAsync(fw.Wishlist.ThemeId);
            }
        }

        return friendWishlists;
    }


    public async Task<FriendWishlist> AddFriendWishlistAsync(int userId, string shareToken, string friendName)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
            throw new ArgumentException("Share token is required");

        if (string.IsNullOrWhiteSpace(friendName))
            throw new ArgumentException("Friend name is required");

        var wishlist = await _wishlistRepository.GetByShareTokenAsync(shareToken);
        if (wishlist == null)
            throw new KeyNotFoundException("Вишлист не найден");

        if (wishlist.UserId == userId)
            throw new InvalidOperationException("Нельзя добавить свой собственный вишлист");

        var existing = await _friendRepository.GetByUserAndWishlistAsync(userId, wishlist.Id);
        if (existing != null)
            throw new InvalidOperationException("Этот вишлист уже добавлен");

        var friendWishlist = new FriendWishlist
        {
            UserId = userId,
            WishlistId = wishlist.Id,
            FriendName = friendName.Trim()
        };

        return await _friendRepository.CreateAsync(friendWishlist);
    }


    public async Task<FriendWishlist> SaveFriendWishlistFromUrlAsync(int userId, string shareToken, string? friendName)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
            throw new ArgumentException("Share token is required");

        var wishlist = await _wishlistRepository.GetByShareTokenAsync(shareToken);
        if (wishlist == null)
            throw new KeyNotFoundException("Вишлист не найден");

        if (wishlist.UserId == userId)
            throw new InvalidOperationException("Нельзя сохранить свой собственный вишлист");

        var existing = await _friendRepository.GetByUserAndWishlistAsync(userId, wishlist.Id);
        if (existing != null)
            throw new InvalidOperationException("Этот вишлист уже сохранен");

        var friendWishlist = new FriendWishlist
        {
            UserId = userId,
            WishlistId = wishlist.Id,
            FriendName = friendName?.Trim() ?? "Друг"
        };

        return await _friendRepository.CreateAsync(friendWishlist);
    }


    public async Task<(FriendWishlist, Wishlist)?> GetFriendWishlistWithItemsForDisplayAsync(int friendWishlistId, int userId)
    {
        var friendWishlist = await _friendRepository.GetByIdAsync(friendWishlistId);
        if (friendWishlist == null || friendWishlist.UserId != userId)
            return null;

        var wishlist = await _wishlistRepository.GetByIdAsync(friendWishlist.WishlistId);
        if (wishlist == null)
            return null;

        wishlist.Theme = await _themeRepository.GetByIdAsync(wishlist.ThemeId);
        var items = await _itemRepository.GetByWishlistIdAsync(wishlist.Id);

        foreach (var item in items)
        {
            item.Links = await _itemRepository.GetItemLinksAsync(item.Id);
        }

        wishlist.Items = items;

        return (friendWishlist, wishlist);
    }


    public async Task<bool> DeleteFriendWishlistAsync(int friendWishlistId, int userId)
    {
        var friendWishlist = await _friendRepository.GetByIdAsync(friendWishlistId);
        if (friendWishlist == null || friendWishlist.UserId != userId)
            return false;

        return await _friendRepository.DeleteAsync(friendWishlistId);
    }
}