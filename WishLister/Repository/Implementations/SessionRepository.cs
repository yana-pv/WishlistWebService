using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WishLister.Models;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;


public class SessionRepository : ISessionRepository
{
    private readonly string _connectionString;

    public SessionRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }

    public async Task<Session> CreateAsync(Session session)
    {
        Console.WriteLine($"[SessionRepository] Connection string: {_connectionString}"); // <-- ЛОГ

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        Console.WriteLine($"[SessionRepository] Connected to DB for session: {session.Id}"); // <-- ЛОГ

        var cmd = new NpgsqlCommand(
            "INSERT INTO sessions (id, user_id, expires_at) VALUES (@id, @user_id, @expires_at)",
            conn);
        cmd.Parameters.AddWithValue("@id", session.Id);
        cmd.Parameters.AddWithValue("@user_id", session.UserId);
        cmd.Parameters.AddWithValue("@expires_at", session.ExpiresAt);

        Console.WriteLine($"[SessionRepository] Before ExecuteNonQueryAsync for session: {session.Id}"); // <-- ЛОГ

        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[SessionRepository] After ExecuteNonQueryAsync for session: {session.Id}"); // <-- ЛОГ

        return session;
    }

    public async Task<Session?> GetByIdAsync(string sessionId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, user_id, expires_at, created_at FROM sessions WHERE id = @id AND expires_at > CURRENT_TIMESTAMP",
            conn);
        cmd.Parameters.AddWithValue("@id", sessionId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Session
            {
                Id = reader.GetString(0),
                UserId = reader.GetInt32(1),
                ExpiresAt = reader.GetDateTime(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }
        return null;
    }

    public async Task<bool> DeleteAsync(string sessionId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM sessions WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", sessionId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM sessions WHERE expires_at <= CURRENT_TIMESTAMP", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
