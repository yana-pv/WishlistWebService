using WishLister.Models.Entities;

namespace WishLister.Repository.Interfaces;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetByIdAsync(string sessionId);
    Task<bool> DeleteAsync(string sessionId);
}
