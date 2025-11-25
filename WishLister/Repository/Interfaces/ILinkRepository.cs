using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;
public interface ILinkRepository
{
    Task<List<ItemLink>> GetByItemIdAsync(int itemId);
    Task<ItemLink> CreateAsync(ItemLink link);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByItemIdAsync(int itemId);

}