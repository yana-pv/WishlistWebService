namespace WishLister.Models.Requests.Wishlists;

public class SaveFriendWishlistRequest
{
    public string ShareToken { get; set; } = string.Empty;
    public string? FriendName { get; set; }
}
