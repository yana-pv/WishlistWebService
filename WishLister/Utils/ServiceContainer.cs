using WishLister.Controllers;
using WishLister.Repository.Implementations;
using WishLister.Repository.Interfaces;
using WishLister.Services;

namespace WishLister.Utils;
public interface IServiceContainer
{
    T GetService<T>();
}


public class ServiceContainer : IServiceContainer
{
    private readonly Dictionary<Type, object> _services = new();

    public ServiceContainer()
    {
        RegisterDependencies();
    }


    private void RegisterDependencies()
    {
        // Репозитории
        var userRepository = new UserRepository();
        var wishlistRepository = new WishlistRepository();
        var itemRepository = new ItemRepository();
        var linkRepository = new LinkRepository();
        var themeRepository = new ThemeRepository();
        var friendRepository = new FriendRepository();
        var sessionRepository = new SessionRepository();

        // Сервисы
        var sessionService = new SessionService(sessionRepository, userRepository);
        var minioService = new MinIOService();
        var cacheService = new CacheService();
        var authService = new AuthService(userRepository, sessionService);
        var userService = new UserService(userRepository, wishlistRepository, itemRepository);
        var themeService = new ThemeService(themeRepository);
        var wishlistService = new WishlistService(wishlistRepository, itemRepository, themeRepository, linkRepository);
        var itemService = new ItemService(itemRepository, linkRepository, wishlistRepository);
        var friendService = new FriendService(friendRepository, wishlistRepository, themeRepository, itemRepository);
        var hybridSearchService = new HybridProductSearchService();
        var linkService = new LinkService(linkRepository, hybridSearchService);

        // Контроллеры 
        var authController = new AuthController(authService);
        var userController = new UserController(userService, sessionService);
        var wishlistController = new WishlistController(wishlistService, themeService, itemRepository, sessionService); 
        var itemController = new ItemController(itemService, minioService, sessionService);
        var friendController = new FriendController(friendService, sessionService);
        var linkController = new LinkController(linkService, sessionService);
        var themeController = new ThemeController(themeService);

        // Репозитории
        _services[typeof(IUserRepository)] = userRepository;
        _services[typeof(IWishlistRepository)] = wishlistRepository;
        _services[typeof(IItemRepository)] = itemRepository;
        _services[typeof(ILinkRepository)] = linkRepository;
        _services[typeof(IThemeRepository)] = themeRepository;
        _services[typeof(IFriendRepository)] = friendRepository;
        _services[typeof(ISessionRepository)] = sessionRepository;

        // Сервисы
        _services[typeof(SessionService)] = sessionService;
        _services[typeof(AuthService)] = authService;
        _services[typeof(MinIOService)] = minioService;
        _services[typeof(CacheService)] = cacheService;
        _services[typeof(UserService)] = userService;
        _services[typeof(ThemeService)] = themeService;
        _services[typeof(WishlistService)] = wishlistService;
        _services[typeof(ItemService)] = itemService;
        _services[typeof(FriendService)] = friendService;
        _services[typeof(HybridProductSearchService)] = hybridSearchService;
        _services[typeof(LinkService)] = linkService;

        // Контроллеры
        _services[typeof(AuthController)] = authController;
        _services[typeof(UserController)] = userController;
        _services[typeof(WishlistController)] = wishlistController;
        _services[typeof(ItemController)] = itemController;
        _services[typeof(FriendController)] = friendController;
        _services[typeof(LinkController)] = linkController;
        _services[typeof(ThemeController)] = themeController;
    }


    public T GetService<T>()
    {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"Service {typeof(T)} not registered.");
    }
}