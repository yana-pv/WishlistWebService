using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;
public interface IItemRepository
{
    Task<WishlistItem?> GetByIdAsync(int id);
    Task<List<WishlistItem>> GetByWishlistIdAsync(int wishlistId);
    Task<WishlistItem> CreateAsync(WishlistItem item);
    Task<WishlistItem> UpdateAsync(WishlistItem item);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReserveItemAsync(int itemId, int userId);
    Task<bool> UnreserveItemAsync(int itemId, int userId); 
    Task<int> GetItemsCountByWishlistAsync(int wishlistId);
    Task<int> GetReservedItemsCountByUserAsync(int userId);
    Task<int> GetItemsCountByUserAsync(int userId);
    Task<List<ItemLink>> GetItemLinksAsync(int itemId);
}