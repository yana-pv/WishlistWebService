namespace WishLister.Models.Requests.Users;

public class DeleteAccountRequest
{
    public string ConfirmPassword { get; set; } = string.Empty;
}
