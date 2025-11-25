namespace WishLister.Models.Requests.Links;

public class AddLinkRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int ItemId { get; set; }  
    public bool IsFromAI { get; set; }
}
