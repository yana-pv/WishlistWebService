using WishLister.Models;
using WishLister.Models.Auth;
using WishLister.Repository.Interfaces;
using WishLister.Utils;

namespace WishLister.Services;
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly SessionService _sessionService;

    public AuthService(IUserRepository userRepository, SessionService sessionService)
    {
        _userRepository = userRepository;
        _sessionService = sessionService;
    }

    public async Task<bool> LogoutAsync(string sessionId)
    {
        return await _sessionService.LogoutAsync(sessionId);
    }

    public async Task<(bool success, string message, string? sessionId)> RegisterAsync(RegisterRequest request)
    {
        var (isValid, validationMessage) = Validators.ValidateUserRegistration(request.Username, request.Email, request.Password, request.ConfirmPassword);
        if (!isValid)
        {
            return (false, validationMessage, null);
        }

        if (await _userRepository.LoginExistsAsync(request.Login))
            return (false, "Пользователь с таким логином уже существует", null);

        if (await _userRepository.EmailExistsAsync(request.Email))
            return (false, "Пользователь с таким email уже существует", null);

        var passwordHash = PasswordHasher.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLower(),
            Login = request.Login.Trim(),
            PasswordHash = passwordHash,
        };

        try
        {
            var createdUser = await _userRepository.CreateAsync(user);
            Console.WriteLine($"[AuthService] User created: {createdUser.Id}"); // <-- ЛОГ

            var result = await _sessionService.CreateSessionAsync(createdUser.Id);
            Console.WriteLine($"[AuthService] Session creation result: {result.success}, Session ID: {result.session?.Id}"); // <-- ЛОГ
            return (result.success, result.message, result.session?.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Error creating user: {ex.Message}"); // <-- ЛОГ
            return (false, $"Ошибка при создании пользователя: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message, string? sessionId)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Логин и пароль обязательны", null);

        var user = await _userRepository.GetByLoginAsync(request.Login.Trim());
        if (user == null)
            return (false, "Неверный логин или пароль", null);

        if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return (false, "Неверный логин или пароль", null);

        Console.WriteLine($"[AuthService] User found for login: {user.Id}"); // <-- ЛОГ

        var result = await _sessionService.CreateSessionAsync(user.Id);
        Console.WriteLine($"[AuthService] Session creation result: {result.success}, Session ID: {result.session?.Id}"); // <-- ЛОГ
        return (result.success, result.message, result.session?.Id);
    }

}