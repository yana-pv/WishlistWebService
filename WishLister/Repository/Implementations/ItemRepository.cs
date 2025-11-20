using Npgsql;
using WishLister.Models;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class ItemRepository : IItemRepository
{
    private readonly string _connectionString;

    public ItemRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }

    public async Task<WishlistItem?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, description, price, image_url, desire_level, comment, " +
            "wishlist_id, is_reserved, reserved_by_user_id, created_at " +
            "FROM wishlist_items WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new WishlistItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                DesireLevel = reader.GetInt32(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6),
                WishlistId = reader.GetInt32(7),
                IsReserved = reader.GetBoolean(8),
                ReservedByUserId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                CreatedAt = reader.GetDateTime(10)
            };
        }
        return null;
    }

    public async Task<List<WishlistItem>> GetByWishlistIdAsync(int wishlistId)
    {
        var items = new List<WishlistItem>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, description, price, image_url, desire_level, comment, " +
            "wishlist_id, is_reserved, reserved_by_user_id, created_at " +
            "FROM wishlist_items WHERE wishlist_id = @wishlistId ORDER BY created_at DESC", conn);
        cmd.Parameters.AddWithValue("@wishlistId", wishlistId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new WishlistItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                DesireLevel = reader.GetInt32(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6),
                WishlistId = reader.GetInt32(7),
                IsReserved = reader.GetBoolean(8),
                ReservedByUserId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                CreatedAt = reader.GetDateTime(10)
            });
        }

        return items;
    }

    public async Task<WishlistItem> CreateAsync(WishlistItem item)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO wishlist_items (title, description, price, image_url, desire_level, comment, wishlist_id) " +
            "VALUES (@title, @description, @price, @imageUrl, @desireLevel, @comment, @wishlistId) " +
            "RETURNING id, created_at", conn);

        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@description", (object?)item.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", (object?)item.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@imageUrl", (object?)item.ImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desireLevel", item.DesireLevel);
        cmd.Parameters.AddWithValue("@comment", (object?)item.Comment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@wishlistId", item.WishlistId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            item.Id = reader.GetInt32(0);
            item.CreatedAt = reader.GetDateTime(1);
        }

        return item;
    }

    public async Task<bool> ReserveItemAsync(int itemId, int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "UPDATE wishlist_items SET is_reserved = true, reserved_by_user_id = @userId " +
            "WHERE id = @id AND is_reserved = false", conn);
        cmd.Parameters.AddWithValue("@id", itemId);
        cmd.Parameters.AddWithValue("@userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> UnreserveItemAsync(int itemId, int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "UPDATE wishlist_items SET is_reserved = false, reserved_by_user_id = NULL " +
            "WHERE id = @id AND reserved_by_user_id = @userId", conn); // Добавлена проверка пользователя
        cmd.Parameters.AddWithValue("@id", itemId);
        cmd.Parameters.AddWithValue("@userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<WishlistItem> UpdateAsync(WishlistItem item)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "UPDATE wishlist_items SET title = @title, description = @description, " +
            "price = @price, image_url = @imageUrl, desire_level = @desireLevel, comment = @comment " +
            "WHERE id = @id", conn);

        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@description", (object?)item.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", (object?)item.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@imageUrl", (object?)item.ImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desireLevel", item.DesireLevel);
        cmd.Parameters.AddWithValue("@comment", (object?)item.Comment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", item.Id);

        await cmd.ExecuteNonQueryAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM wishlist_items WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<int> GetItemsCountByWishlistAsync(int wishlistId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM wishlist_items WHERE wishlist_id = @wishlistId", conn);
        cmd.Parameters.AddWithValue("@wishlistId", wishlistId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return (int)count;
    }

    public async Task<int> GetReservedItemsCountByUserAsync(int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM wishlist_items WHERE reserved_by_user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return (int)count;
    }

    public async Task<int> GetItemsCountByUserAsync(int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            @"SELECT COUNT(*) FROM wishlist_items wi 
              JOIN wishlists w ON w.id = wi.wishlist_id 
              WHERE w.user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        return (int)count;
    }

    public async Task<List<ItemLink>> GetItemLinksAsync(int itemId)
    {
        var links = new List<ItemLink>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, item_id, url, title, price, is_from_ai, is_selected " +
            "FROM item_links WHERE item_id = @itemId", conn);
        cmd.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            links.Add(new ItemLink
            {
                Id = reader.GetInt32(0),
                ItemId = reader.GetInt32(1),
                Url = reader.GetString(2),
                Title = reader.IsDBNull(3) ? null : reader.GetString(3),
                Price = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                IsFromAI = reader.GetBoolean(5),
                IsSelected = reader.GetBoolean(6)
            });
        }

        return links;
    }
}