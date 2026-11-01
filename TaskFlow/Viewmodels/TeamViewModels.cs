using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels;

// Ekip kurma formu. Team sınıfını doğrudan bağlamıyoruz:
// CreatedAt, Members gibi alanları kullanıcı formdan değiştiremesin (overposting).
public class TeamCreateViewModel
{
    [Required(ErrorMessage = "Ekip adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ekip adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Ekip Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }
}

// "Ekiplerim" listesindeki bir satır
public class TeamListItemViewModel
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MyRole { get; set; } = string.Empty; // giriş yapan kullanıcının bu ekipteki rolü
    public int MemberCount { get; set; }
}

// Ekip detay sayfasının verisi
public class TeamDetailsViewModel
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Giriş yapan kişi bu ekibi yönetebilir mi? (üye ekleme formunu göstermek için)
    public bool CanManage { get; set; }

    public List<TeamMemberItemViewModel> Members { get; set; } = new();
}

// Üye tablosundaki bir satır
public class TeamMemberItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TeamRole { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

// Üye ekleme formu: lider, kullanıcı adı ya da e-posta yazar
public class AddMemberViewModel
{
    public int TeamId { get; set; }

    [Required(ErrorMessage = "Kullanıcı adı veya e-posta girin.")]
    [Display(Name = "Kullanıcı adı veya e-posta")]
    public string UserNameOrEmail { get; set; } = string.Empty;
}