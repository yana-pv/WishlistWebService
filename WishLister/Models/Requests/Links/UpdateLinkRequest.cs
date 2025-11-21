namespace WishLister.Models.Requests.Links;

public class UpdateLinkRequest
{
    public string? Title { get; set; }
    public decimal? Price { get; set; }
    public bool IsSelected { get; set; }
    public int ItemId { get; set; }
}
