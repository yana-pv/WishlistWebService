using Npgsql;
using WishLister.Models;
using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class FriendRepository : IFriendRepository
{
    private readonly string _connectionString;

    public FriendRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }

    public async Task<FriendWishlist?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, user_id, wishlist_id, friend_name, created_at " +
            "FROM friend_wishlists WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new FriendWishlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                WishlistId = reader.GetInt32(2),
                FriendName = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }
        return null;
    }


    public async Task<List<FriendWishlist>> GetByUserIdAsyncWithWishlist(int userId)
    {
        var friendWishlists = new List<FriendWishlist>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT fw.id, fw.user_id, fw.wishlist_id, fw.friend_name, fw.created_at, " +
            "w.title, w.description, w.event_date, w.theme_id " +
            "FROM friend_wishlists fw " +
            "JOIN wishlists w ON w.id = fw.wishlist_id " +
            "WHERE fw.user_id = @userId " +
            "ORDER BY fw.created_at DESC", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var friendWishlist = new FriendWishlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                WishlistId = reader.GetInt32(2),
                FriendName = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };

            friendWishlist.Wishlist = new Wishlist
            {
                Id = friendWishlist.WishlistId,
                Title = reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                EventDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                ThemeId = reader.GetInt32(8)
            };

            friendWishlists.Add(friendWishlist);
        }

        return friendWishlists;
    }


    public async Task<List<FriendWishlist>> GetByUserIdAsync(int userId)
    {
        var friendWishlists = new List<FriendWishlist>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT fw.id, fw.user_id, fw.wishlist_id, fw.friend_name, fw.created_at, " +
            "w.title, w.description, w.event_date, w.theme_id " +
            "FROM friend_wishlists fw " +
            "JOIN wishlists w ON w.id = fw.wishlist_id " +
            "WHERE fw.user_id = @userId " +
            "ORDER BY fw.created_at DESC", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var friendWishlist = new FriendWishlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                WishlistId = reader.GetInt32(2),
                FriendName = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };

            friendWishlist.Wishlist = new Wishlist
            {
                Id = friendWishlist.WishlistId,
                Title = reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                EventDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                ThemeId = reader.GetInt32(8)
            };

            friendWishlists.Add(friendWishlist);
        }

        return friendWishlists;
    }


    public async Task<FriendWishlist?> GetByUserAndWishlistAsync(int userId, int wishlistId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, user_id, wishlist_id, friend_name, created_at " +
            "FROM friend_wishlists WHERE user_id = @userId AND wishlist_id = @wishlistId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@wishlistId", wishlistId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new FriendWishlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                WishlistId = reader.GetInt32(2),
                FriendName = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }
        return null;
    }


    public async Task<FriendWishlist> CreateAsync(FriendWishlist friendWishlist)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO friend_wishlists (user_id, wishlist_id, friend_name) " +
            "VALUES (@userId, @wishlistId, @friendName) " +
            "RETURNING id, created_at", conn);

        cmd.Parameters.AddWithValue("@userId", friendWishlist.UserId);
        cmd.Parameters.AddWithValue("@wishlistId", friendWishlist.WishlistId);
        cmd.Parameters.AddWithValue("@friendName", friendWishlist.FriendName);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            friendWishlist.Id = reader.GetInt32(0);
            friendWishlist.CreatedAt = reader.GetDateTime(1);
        }

        return friendWishlist;
    }


    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM friend_wishlists WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }


    public async Task<bool> ExistsAsync(int userId, int wishlistId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM friend_wishlists WHERE user_id = @userId AND wishlist_id = @wishlistId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@wishlistId", wishlistId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }
}