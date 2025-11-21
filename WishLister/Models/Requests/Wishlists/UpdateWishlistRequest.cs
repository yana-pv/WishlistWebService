namespace WishLister.Models.Requests.Wishlists;

public class UpdateWishlistRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public int ThemeId { get; set; }
}