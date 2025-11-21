using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByLoginAsync(string login);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> LoginExistsAsync(string login);
    Task<bool> EmailExistsAsync(string email);
}