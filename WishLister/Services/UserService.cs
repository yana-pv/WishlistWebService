using WishLister.Models.Entities;
using WishLister.Models.Responses;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Services;
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IItemRepository _itemRepository;


    public UserService(IUserRepository userRepository, IWishlistRepository wishlistRepository, IItemRepository itemRepository)
    {
        _userRepository = userRepository;
        _wishlistRepository = wishlistRepository;
        _itemRepository = itemRepository;
    }


    public async Task<User?> GetUserProfileAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }


    public async Task<User> UpdateUserProfileAsync(int userId, string username, string email, string? avatarUrl)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Пользователь не найден");
        }

        if (!Validators.IsValidUsername(username))
        {
            throw new ArgumentException("Некорректное имя пользователя");
        }

        if (!Validators.IsValidEmail(email))
        {
            throw new ArgumentException("Некорректный email");
        }

        if (user.Email != email)
        {
            if (await _userRepository.EmailExistsAsync(email))
                throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        user.Username = username;
        user.Email = email;
        user.AvatarUrl = avatarUrl;

        return await _userRepository.UpdateAsync(user);
    }


    public async Task DeleteAccountAsync(int userId, string confirmPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Пользователь не найден");
        }

        if (!PasswordHasher.VerifyPassword(confirmPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Неверный пароль для подтверждения");
        }

        var success = await _userRepository.DeleteAsync(userId);
        if (!success)
        {
            throw new InvalidOperationException("Не удалось удалить аккаунт");
        }
    }


    public async Task<UserStats> GetUserStatsAsync(int userId)
    {
        var wishlistsCount = await _wishlistRepository.GetWishlistsCountByUserAsync(userId);
        var itemsCount = await _itemRepository.GetItemsCountByUserAsync(userId);
        var reservedItemsCount = await _itemRepository.GetReservedItemsCountByUserAsync(userId);

        return new UserStats
        {
            WishlistsCount = wishlistsCount,
            ItemsCount = itemsCount,
            ReservedItemsCount = reservedItemsCount
        };
    }
}


