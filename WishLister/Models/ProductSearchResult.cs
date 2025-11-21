namespace WishLister.Models;
public class ProductSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsDirectLink { get; set; }
}