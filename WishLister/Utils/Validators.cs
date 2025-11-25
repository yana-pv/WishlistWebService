using System.Text.RegularExpressions;

namespace WishLister.Utils;

public static class Validators
{
    public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string UsernameRegex = @"^[a-zA-Z0-9_]{3,20}$";


    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return Regex.IsMatch(email, EmailRegex, RegexOptions.IgnoreCase);
        }

        catch
        {
            return false;
        }
    }


    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        return Regex.IsMatch(username, UsernameRegex);
    }


    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return false;

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);

        return hasLetter && hasDigit;
    }


    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }


    public static bool IsValidPrice(decimal? price)
    {
        if (!price.HasValue)
            return true;

        return price >= 0 && price <= 9999999.99m;
    }


    public static bool IsValidDesireLevel(int level)
    {
        return level >= 1 && level <= 3;
    }


    public static (bool isValid, string message) ValidateUserRegistration(string username, string email, string password, string confirmPassword)
    {
        if (!IsValidUsername(username))
            return (false, "Имя пользователя должно содержать 3-20 символов (латинские буквы, цифры, подчеркивания)");

        if (!IsValidEmail(email))
            return (false, "Введите корректный email адрес");

        if (!IsValidPassword(password))
            return (false, "Пароль должен содержать минимум 6 символов, включая буквы и цифры");

        if (password != confirmPassword)
            return (false, "Пароли не совпадают");

        return (true, "Валидация успешна");
    }


    public static (bool isValid, string message) ValidateWishlist(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 2)
            return (false, "Название вишлиста должно содержать минимум 2 символа");

        if (title.Length > 100)
            return (false, "Название вишлиста не должно превышать 100 символов");

        if (!string.IsNullOrEmpty(description) && description.Length > 500)
            return (false, "Описание не должно превышать 500 символов");

        return (true, "Валидация успешна");
    }


    public static (bool isValid, string message) ValidateItem(string title, decimal? price, int desireLevel)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 2)
            return (false, "Название товара должно содержать минимум 2 символа");

        if (title.Length > 100)
            return (false, "Название товара не должно превышать 100 символов");

        if (!IsValidPrice(price))
            return (false, "Цена должна быть в диапазоне от 0 до 9,999,999.99");

        if (!IsValidDesireLevel(desireLevel))
            return (false, "Уровень желания должен быть от 1 до 3");

        return (true, "Валидация успешна");
    }
}