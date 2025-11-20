namespace WishLister.Models;
public class WishlistItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public int DesireLevel { get; set; } 
    public string? Comment { get; set; }
    public int WishlistId { get; set; }
    public bool IsReserved { get; set; }
    public int? ReservedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ItemLink> Links { get; set; } = new();
}