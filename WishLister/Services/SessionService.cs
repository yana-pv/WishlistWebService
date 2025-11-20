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
            Console.WriteLine($"[SessionService] User {userId} not found"); // <-- ЛОГ
            return (false, "Пользователь не найден", null);
        }

        Console.WriteLine($"[SessionService] Creating session for user: {userId}"); // <-- ЛОГ

        var session = new Session
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 дней
        };

        var createdSession = await _sessionRepository.CreateAsync(session);
        Console.WriteLine($"[SessionService] Session created successfully: {createdSession.Id} for user {userId}"); // <-- ЛОГ
        return (true, "Сессия создана", createdSession);
    }

    public async Task<Session?> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            return null;

        // Автоматическое продление сессии
        if (session.ExpiresAt > DateTime.UtcNow.AddHours(1)) // если больше 1 часа до истечения
        {
            session.ExpiresAt = DateTime.UtcNow.AddDays(7);
            // тут можно обновить в БД, если хотите продлевать
        }

        return session;
    }

    public async Task<bool> LogoutAsync(string sessionId)
    {
        return await _sessionRepository.DeleteAsync(sessionId);
    }
}
