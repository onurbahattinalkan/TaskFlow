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
