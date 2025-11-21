namespace WishLister.Models.Requests.Wishlists;

public class AddFriendWishlistRequest
{
    public string ShareToken { get; set; } = string.Empty;
    public string FriendName { get; set; } = string.Empty;
}
