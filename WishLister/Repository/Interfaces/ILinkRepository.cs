using WishLister.Models;

namespace WishLister.Repository.Interfaces;
public interface ILinkRepository
{
    Task<List<ItemLink>> GetByItemIdAsync(int itemId);
    Task<ItemLink> CreateAsync(ItemLink link);
    Task<bool> DeleteAsync(int id);
    Task<bool> SetSelectedLinkAsync(int itemId, int linkId);
    Task<bool> DeleteByItemIdAsync(int itemId);

}