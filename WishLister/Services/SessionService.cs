using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WishLister.Models;
using WishLister.Repository.Interfaces;

namespace WishLister.Services;

public class SessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;

    public SessionService(ISessionRepository sessionRepository, IUserRepository userRepository)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
    }


    public async Task<(bool success, string message, Session? session)> CreateSessionAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "Пользователь не найден", null);
        }


        var session = new Session
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 дней
        };

        var createdSession = await _sessionRepository.CreateAsync(session);

        return (true, "Сессия создана", createdSession);
    }


    public async Task<Session?> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            return null;

        if (session.ExpiresAt > DateTime.UtcNow.AddHours(1)) // если больше 1 часа до истечения
        {
            session.ExpiresAt = DateTime.UtcNow.AddDays(7);
        }

        return session;
    }


    public async Task<bool> LogoutAsync(string sessionId)
    {
        return await _sessionRepository.DeleteAsync(sessionId);
    }
}
