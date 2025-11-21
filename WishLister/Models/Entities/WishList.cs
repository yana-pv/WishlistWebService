namespace WishLister.Models.Entities;
public class Wishlist
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public int ThemeId { get; set; }
    public int UserId { get; set; }
    public string ShareToken { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WishlistItem> Items { get; set; } = new();
    public Theme Theme { get; set; } = new();
}