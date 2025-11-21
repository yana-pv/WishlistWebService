namespace WishLister.Models.Entities;
public class Theme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string ButtonColor { get; set; } = string.Empty;
}