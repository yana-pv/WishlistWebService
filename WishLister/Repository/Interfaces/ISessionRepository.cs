using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WishLister.Models;

namespace WishLister.Repository.Interfaces;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetByIdAsync(string sessionId);
    Task<bool> DeleteAsync(string sessionId);
    Task CleanupExpiredSessionsAsync();
}
