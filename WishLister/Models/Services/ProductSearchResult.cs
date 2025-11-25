namespace WishLister.Models.Services;
public class ProductSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDirectLink { get; set; }
}