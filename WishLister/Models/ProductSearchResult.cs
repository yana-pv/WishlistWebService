namespace WishLister.Models;
public class ProductSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }

    // Новое поле - является ли ссылка прямой на товар
    public bool IsDirectLink { get; set; }

    // Новое поле - рейтинг товара
    public double? Rating { get; set; }
}