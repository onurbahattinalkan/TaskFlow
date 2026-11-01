namespace TaskFlow.ViewModels;

// Admin'in kullanıcı listesindeki tek bir satır.
// ApplicationUser'da roller yok (ayrı tabloda tutuluyor), o yüzden ikisini burada birleştiriyoruz.
public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}