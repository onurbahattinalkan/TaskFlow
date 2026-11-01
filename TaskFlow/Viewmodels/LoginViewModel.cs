using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels;

// Giriş formunun verileri
public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    // İşaretlenirse çerez kalıcı olur (tarayıcı kapanınca da giriş açık kalır).
    // İşaretlenmezse tarayıcı kapanınca çıkış yapılmış olur.
    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}