using Npgsql;
using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class LinkRepository : ILinkRepository
{
    private readonly string _connectionString;

    public LinkRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }


    public async Task<List<ItemLink>> GetByItemIdAsync(int itemId)
    {
        var links = new List<ItemLink>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, url, title, is_from_ai, item_id, created_at " +
            "FROM item_links WHERE item_id = @itemId ORDER BY created_at", conn); // ← убран ORDER BY is_selected
        cmd.Parameters.AddWithValue("@itemId", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            links.Add(new ItemLink
            {
                Id = reader.GetInt32(0),
                Url = reader.GetString(1),
                Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsFromAI = reader.GetBoolean(3), 
                ItemId = reader.GetInt32(4), 
                CreatedAt = reader.GetDateTime(5) 
            });
        }

        return links;
    }


    public async Task<ItemLink> CreateAsync(ItemLink link)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO item_links (url, title, is_from_ai, item_id) " +
            "VALUES (@url, @title, @isFromAI, @itemId) " +
            "RETURNING id, created_at", conn);

        cmd.Parameters.AddWithValue("@url", link.Url);
        cmd.Parameters.AddWithValue("@title", (object?)link.Title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@isFromAI", link.IsFromAI);
        cmd.Parameters.AddWithValue("@itemId", link.ItemId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            link.Id = reader.GetInt32(0);
            link.CreatedAt = reader.GetDateTime(1);
        }

        return link;
    }


    public async Task<bool> DeleteByItemIdAsync(int itemId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM item_links WHERE item_id = @itemId", conn);
        cmd.Parameters.AddWithValue("@itemId", itemId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }


    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM item_links WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }
}