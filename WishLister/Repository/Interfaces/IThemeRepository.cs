using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;
public interface IThemeRepository
{
    Task<Theme?> GetByIdAsync(int id);
    Task<List<Theme>> GetAllAsync();
}