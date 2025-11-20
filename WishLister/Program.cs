// Program.cs
using System.Net;
using System.Text.Json;
using WishLister.Controllers;
using WishLister.Middleware;
using WishLister.Repository.Interfaces;
using WishLister.Services;
using WishLister.Utils;

// Используем ServiceContainer для получения ВСЕГО
var container = new ServiceContainer();

// Получаем ВСЕ контроллеры ИЗ КОНТЕЙНЕРА (не создаем вручную!)
var authController = container.GetService<AuthController>();
var userController = container.GetService<UserController>();
var wishlistController = container.GetService<WishlistController>();
var itemController = container.GetService<ItemController>();
var friendController = container.GetService<FriendController>();
var linkController = container.GetService<LinkController>();
var themeController = container.GetService<ThemeController>();

// Получаем нужные сервисы
var sessionService = container.GetService<SessionService>();
var userRepository = container.GetService<IUserRepository>();

// --- ПОСТРОЕНИЕ ЦЕПОЧКИ MIDDLEWARE В ПРАВИЛЬНОМ ПОРЯДКЕ ---
RequestDelegate requestDelegate = async (context) =>
{
    context.Response.StatusCode = 404;
    var errorResponse = new { status = "error", message = "Not Found" };
    var json = JsonSerializer.Serialize(errorResponse);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    await context.Response.OutputStream.WriteAsync(bytes);
};

// ПРАВИЛЬНЫЙ ПОРЯДОК:
// 1. Логирование
requestDelegate = new LoggingMiddleware(requestDelegate).InvokeAsync;

// 2. Аутентификация (ДО роутинга!)
requestDelegate = new AuthMiddleware(requestDelegate, sessionService, userRepository).InvokeAsync; // Обновлено

// 3. Маршрутизация API
requestDelegate = new RoutingMiddleware(requestDelegate, authController, userController,
    wishlistController, itemController, friendController, linkController, themeController).InvokeAsync;

// 4. Статические файлы
requestDelegate = new StaticFilesMiddleware(requestDelegate).InvokeAsync;

// 5. Обработчик ошибок
var finalMiddleware = new ErrorHandlingMiddleware(requestDelegate).InvokeAsync;

// Создание БД таблиц
var dbContext = new DbContext();
await dbContext.CreateTablesAsync(CancellationToken.None);

// Настройка HttpListener
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();

Console.WriteLine("WishLister server started: http://localhost:5000/");
Console.WriteLine("Database tables created successfully");

// Обработка запросов
while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();

    _ = Task.Run(async () =>
    {
        try
        {
            await finalMiddleware(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error outside middleware: {ex.Message}");
            context.Response.StatusCode = 500;
            var errorResponse = new { status = "error", message = "Internal server error" };
            var json = JsonSerializer.Serialize(errorResponse);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await context.Response.OutputStream.WriteAsync(bytes);
        }
        finally
        {
            context.Response.Close();
        }
    });
}