namespace WishLister.Models.Entities;
public class ReservedItem
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int ReservedByUserId { get; set; }
    public DateTime ReservedAt { get; set; }
    public string? Message { get; set; }
}