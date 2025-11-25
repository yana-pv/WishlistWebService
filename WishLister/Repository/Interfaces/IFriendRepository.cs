using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;
public interface IFriendRepository
{
    Task<FriendWishlist?> GetByIdAsync(int id);
    Task<FriendWishlist?> GetByUserAndWishlistAsync(int userId, int wishlistId);
    Task<FriendWishlist> CreateAsync(FriendWishlist friendWishlist);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int userId, int wishlistId);
    Task<List<FriendWishlist>> GetByUserIdAsyncWithWishlist(int userId);
}