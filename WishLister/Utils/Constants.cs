namespace WishLister.Utils;
public static class Constants
{
    public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string UsernameRegex = @"^[a-zA-Z0-9_]{3,20}$";
    public const string PhoneRegex = @"^\+?[1-9]\d{1,14}$";

    public const string RoleUser = "user";
    public const string RoleAdmin = "admin";
    public const string RoleGuest = "guest";

    public const int MaxWishlistsPerUser = 50;
    public const int MaxItemsPerWishlist = 100;
    public const int MaxLinksPerItem = 10;
    public const int MaxImageSizeMB = 5;

    public const string DefaultWelcomeMessage = "Добро пожаловать в WishLister!";
    public const string ItemReservedMessage = "Подарок забронирован";
    public const string ItemUnreservedMessage = "Бронирование отменено";

    public const int ErrorValidation = 400;
    public const int ErrorUnauthorized = 401;
    public const int ErrorForbidden = 403;
    public const int ErrorNotFound = 404;
    public const int ErrorInternal = 500;
}