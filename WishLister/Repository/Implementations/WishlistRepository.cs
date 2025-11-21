using Npgsql;
using WishLister.Models;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class WishlistRepository : IWishlistRepository
{
    private readonly string _connectionString;

    public WishlistRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }


    public async Task<Wishlist?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, description, event_date, theme_id, user_id, share_token, created_at, updated_at " +
            "FROM wishlists WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Wishlist
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                EventDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                ThemeId = reader.GetInt32(4),
                UserId = reader.GetInt32(5),
                ShareToken = reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            };
        }
        return null;
    }


    public async Task<Wishlist?> GetByShareTokenAsync(string shareToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, description, event_date, theme_id, user_id, share_token, created_at, updated_at " +
            "FROM wishlists WHERE share_token = @shareToken", conn);
        cmd.Parameters.AddWithValue("@shareToken", shareToken);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Wishlist
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                EventDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                ThemeId = reader.GetInt32(4),
                UserId = reader.GetInt32(5),
                ShareToken = reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            };
        }
        return null;
    }


    public async Task<List<Wishlist>> GetByUserIdAsync(int userId)
    {
        var wishlists = new List<Wishlist>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, description, event_date, theme_id, user_id, share_token, created_at, updated_at " +
            "FROM wishlists WHERE user_id = @userId ORDER BY created_at DESC", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            wishlists.Add(new Wishlist
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                EventDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                ThemeId = reader.GetInt32(4),
                UserId = reader.GetInt32(5),
                ShareToken = reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.GetDateTime(8)
            });
        }

        return wishlists;
    }


    public async Task<int> GetWishlistsCountByUserAsync(int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM wishlists WHERE user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return (int)count;
    }


    public async Task<Wishlist> CreateAsync(Wishlist wishlist)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO wishlists (title, description, event_date, theme_id, user_id, share_token) " +
            "VALUES (@title, @description, @eventDate, @themeId, @userId, @shareToken) " +
            "RETURNING id, created_at, updated_at", conn);

        cmd.Parameters.AddWithValue("@title", wishlist.Title);
        cmd.Parameters.AddWithValue("@description", (object?)wishlist.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@eventDate", (object?)wishlist.EventDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@themeId", wishlist.ThemeId);
        cmd.Parameters.AddWithValue("@userId", wishlist.UserId);
        cmd.Parameters.AddWithValue("@shareToken", wishlist.ShareToken);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            wishlist.Id = reader.GetInt32(0);
            wishlist.CreatedAt = reader.GetDateTime(1);
            wishlist.UpdatedAt = reader.GetDateTime(2);
        }

        return wishlist;
    }


    public async Task<bool> UserOwnsWishlistAsync(int wishlistId, int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM wishlists WHERE id = @id AND user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@id", wishlistId);
        cmd.Parameters.AddWithValue("@userId", userId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }


    public async Task<Wishlist> UpdateAsync(Wishlist wishlist)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "UPDATE wishlists SET title = @title, description = @description, " +
            "event_date = @eventDate, theme_id = @themeId, updated_at = CURRENT_TIMESTAMP " +
            "WHERE id = @id RETURNING updated_at", conn);

        cmd.Parameters.AddWithValue("@title", wishlist.Title);
        cmd.Parameters.AddWithValue("@description", (object?)wishlist.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@eventDate", (object?)wishlist.EventDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@themeId", wishlist.ThemeId);
        cmd.Parameters.AddWithValue("@id", wishlist.Id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            wishlist.UpdatedAt = reader.GetDateTime(0);
        }

        return wishlist;
    }


    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM wishlists WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }
}