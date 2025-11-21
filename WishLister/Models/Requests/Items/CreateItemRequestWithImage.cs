using WishLister.Models.Requests.Links;

namespace WishLister.Models.Requests.Items;

public class CreateItemRequestWithImage
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageData { get; set; }
    public int DesireLevel { get; set; } = 1;
    public string? Comment { get; set; }
    public int WishlistId { get; set; }
    public List<CreateLinkRequest>? Links { get; set; }
}
