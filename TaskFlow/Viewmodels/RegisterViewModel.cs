using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels;

// Kayıt formunun verileri. Sadece bu form için var, veritabanına doğrudan yazılmaz.
public class RegisterViewModel
{
    // Giriş bu adresle yapılacak, onay maili buraya gidecek
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    // Uygulama içinde görünecek ad (görev atama, yorumlar vb.)
    // Benzersizliğini Identity kontrol eder; izin verilen karakterleri 1.1'de belirledik.
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-20 karakter olmalıdır.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    // Zeynep'in ApplicationUser sınıfında zorunlu alan
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(30, ErrorMessage = "Ad soyad en fazla 30 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    // [DataType(Password)]: View'da input type="password" olarak çizilir (karakterler gizlenir)
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    // [Compare]: bu alanın değeri "Password" ile AYNI olmak zorunda
    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre (Tekrar)")]
    public string ConfirmPassword { get; set; } = string.Empty;
}