using Npgsql;
using WishLister.Models.Entities;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Repository.Implementations;
public class ThemeRepository : IThemeRepository
{
    private readonly string _connectionString;

    public ThemeRepository()
    {
        _connectionString = ConfigHelper.GetConnectionString();
    }


    public async Task<Theme?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, name, color, background, button_color FROM themes WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Theme
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Color = reader.GetString(2),
                Background = reader.GetString(3),
                ButtonColor = reader.GetString(4)
            };
        }
        return null;
    }


    public async Task<List<Theme>> GetAllAsync()
    {
        var themes = new List<Theme>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, name, color, background, button_color FROM themes ORDER BY id", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            themes.Add(new Theme
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Color = reader.GetString(2),
                Background = reader.GetString(3),
                ButtonColor = reader.GetString(4)
            });
        }

        return themes;
    }
}