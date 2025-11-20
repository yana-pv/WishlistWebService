namespace WishLister.Models;
public class ItemLink
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public decimal? Price { get; set; }
    public bool IsFromAI { get; set; }
    public bool IsSelected { get; set; }
    public int ItemId { get; set; }
    public DateTime CreatedAt { get; set; }
}