using System.Net;
using System.Text.Json;
using WishLister.Controllers;
using WishLister.Middleware;
using WishLister.Repository.Interfaces;
using WishLister.Services;
using WishLister.Utils;

var container = new ServiceContainer();

var authController = container.GetService<AuthController>();
var userController = container.GetService<UserController>();
var wishlistController = container.GetService<WishlistController>();
var itemController = container.GetService<ItemController>();
var friendController = container.GetService<FriendController>();
var linkController = container.GetService<LinkController>();
var themeController = container.GetService<ThemeController>();

var sessionService = container.GetService<SessionService>();
var userRepository = container.GetService<IUserRepository>();

RequestDelegate requestDelegate = async (context) =>
{
    context.Response.StatusCode = 404;
    var errorResponse = new { status = "error", message = "Not Found" };
    var json = JsonSerializer.Serialize(errorResponse);
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    await context.Response.OutputStream.WriteAsync(bytes);
};

requestDelegate = new LoggingMiddleware(requestDelegate).InvokeAsync;
requestDelegate = new AuthMiddleware(requestDelegate, sessionService, userRepository).InvokeAsync; 
requestDelegate = new RoutingMiddleware(requestDelegate, authController, userController,
    wishlistController, itemController, friendController, linkController, themeController).InvokeAsync;
requestDelegate = new StaticFilesMiddleware(requestDelegate).InvokeAsync;
var finalMiddleware = new ErrorHandlingMiddleware(requestDelegate).InvokeAsync;

// Создание БД таблиц
var dbContext = new DbContext();
await dbContext.CreateTablesAsync(CancellationToken.None);

// Настройка HttpListener
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();

Console.WriteLine("WishLister server started: http://localhost:5000/");

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