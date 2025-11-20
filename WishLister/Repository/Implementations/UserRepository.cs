// WishLister.Repository.Implementations\UserRepository.cs
using Npgsql;
using WishLister.Models;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository()
    {
        _connectionString = Utils.ConfigHelper.GetConnectionString();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, username, email, login, password_hash, avatar_url, phone, created_at, updated_at " +
            "FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                Login = reader.GetString(3),
                PasswordHash = reader.GetString(4),
                AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            };
        }
        return null;
    }

    public async Task<User?> GetByLoginAsync(string login)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, username, email, login, password_hash, avatar_url, phone, created_at, updated_at " +
            "FROM users WHERE login = @login", conn);
        cmd.Parameters.AddWithValue("@login", login);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                Login = reader.GetString(3),
                PasswordHash = reader.GetString(4),
                AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            };
        }
        return null;
    }

    public async Task<User> CreateAsync(User user)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO users (username, email, login, password_hash, avatar_url, phone) " +
            "VALUES (@username, @email, @login, @password_hash, @avatar_url, @phone) " +
            "RETURNING id, created_at, updated_at", conn);

        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@login", user.Login);
        cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@avatar_url", (object?)user.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)user.Phone ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            user.Id = reader.GetInt32(0);
            user.CreatedAt = reader.GetDateTime(1);
            user.UpdatedAt = reader.GetDateTime(2);
        }

        return user;
    }

    public async Task<bool> LoginExistsAsync(string login)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login = @login", conn);
        cmd.Parameters.AddWithValue("@login", login);
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @email", conn);
        cmd.Parameters.AddWithValue("@email", email);
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, username, email, login, password_hash, avatar_url, phone, created_at, updated_at " +
            "FROM users WHERE email = @email", conn); // Запрос по email
        cmd.Parameters.AddWithValue("@email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                Login = reader.GetString(3),
                PasswordHash = reader.GetString(4),
                AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            };
        }
        return null; // Пользователь не найден
    }

    public async Task<User> UpdateAsync(User user)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "UPDATE users SET username = @username, email = @email, login = @login, " +
            "avatar_url = @avatar_url, phone = @phone, updated_at = CURRENT_TIMESTAMP " +
            "WHERE id = @id RETURNING updated_at", conn); // Обновляем поля, кроме пароля

        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@login", user.Login);
        cmd.Parameters.AddWithValue("@avatar_url", (object?)user.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)user.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", user.Id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            user.UpdatedAt = reader.GetDateTime(0); // Обновляем время в объекте
        }
        else
        {
            throw new KeyNotFoundException($"User with ID {user.Id} not found for update.");
        }

        return user;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }
}