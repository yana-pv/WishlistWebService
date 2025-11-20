using WishLister.Models;

namespace WishLister.Repository.Interfaces;
public interface IWishlistRepository
{
    Task<Wishlist?> GetByIdAsync(int id);
    Task<Wishlist?> GetByShareTokenAsync(string shareToken);
    Task<List<Wishlist>> GetByUserIdAsync(int userId);
    Task<Wishlist> CreateAsync(Wishlist wishlist);
    Task<Wishlist> UpdateAsync(Wishlist wishlist);
    Task<bool> DeleteAsync(int id);
    Task<bool> UserOwnsWishlistAsync(int wishlistId, int userId);
    Task<int> GetWishlistsCountByUserAsync(int userId);
}