namespace WishLister.Models;
public class FriendWishlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int WishlistId { get; set; }
    public string FriendName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Wishlist Wishlist { get; set; } = new();
}