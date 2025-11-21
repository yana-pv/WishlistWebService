using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace WishLister.Utils;
public class DbContext
{
    public static string ConnectionString => ConfigHelper.GetConnectionString();

    public async Task CreateTablesAsync(CancellationToken token)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync(token);

        string sql = @"
        CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY,
            username TEXT NOT NULL,
            email TEXT UNIQUE NOT NULL,
            login TEXT UNIQUE NOT NULL,
            password_hash TEXT NOT NULL,
            avatar_url TEXT,
            phone TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS themes (
            id SERIAL PRIMARY KEY,
            name TEXT NOT NULL,
            color TEXT NOT NULL,
            background TEXT NOT NULL,
            button_color TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS wishlists (
            id SERIAL PRIMARY KEY,
            title TEXT NOT NULL,
            description TEXT,
            event_date DATE,
            theme_id INT REFERENCES themes(id),
            user_id INT REFERENCES users(id) ON DELETE CASCADE,
            share_token TEXT UNIQUE NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS wishlist_items (
            id SERIAL PRIMARY KEY,
            title TEXT NOT NULL,
            description TEXT,
            price DECIMAL(10,2),
            image_url TEXT,
            desire_level INT CHECK (desire_level BETWEEN 1 AND 3),
            comment TEXT,
            wishlist_id INT REFERENCES wishlists(id) ON DELETE CASCADE,
            is_reserved BOOLEAN DEFAULT FALSE,
            reserved_by_user_id INT REFERENCES users(id) ON DELETE SET NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS item_links (
            id SERIAL PRIMARY KEY,
            url TEXT NOT NULL,
            title TEXT,
            price DECIMAL(10,2),
            is_from_ai BOOLEAN DEFAULT FALSE,
            is_selected BOOLEAN DEFAULT FALSE,
            item_id INT REFERENCES wishlist_items(id) ON DELETE CASCADE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS friend_wishlists (
            id SERIAL PRIMARY KEY,
            user_id INT REFERENCES users(id) ON DELETE CASCADE,
            wishlist_id INT REFERENCES wishlists(id) ON DELETE CASCADE,
            friend_name TEXT NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(user_id, wishlist_id)
        );

        CREATE TABLE IF NOT EXISTS sessions (
            id TEXT PRIMARY KEY,
            user_id INT REFERENCES users(id) ON DELETE CASCADE,
            expires_at TIMESTAMP NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        INSERT INTO themes (name, color, background, button_color) VALUES
        ('Розовая', '#ff69b4', '#fff0f5', '#ff1493'),
        ('Синяя', '#1e90ff', '#f0f8ff', '#0000ff'),
        ('Зеленая', '#32cd32', '#f0fff0', '#008000'),
        ('Фиолетовая', '#8a2be2', '#f8f0ff', '#9400d3')
        ON CONFLICT DO NOTHING;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(token);
    }
}