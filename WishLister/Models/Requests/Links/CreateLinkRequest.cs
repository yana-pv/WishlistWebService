namespace WishLister.Models.Requests.Links;

public class CreateLinkRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public bool IsFromAI { get; set; }
}