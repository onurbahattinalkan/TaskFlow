using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Services;

// Identity'nin İngilizce hata mesajlarını Türkçe yapar.
// IdentityErrorDescriber'dan türetip, ihtiyacımız olan metotları override ediyoruz.
// Code'u Identity'nin orijinal metodundan alıyoruz (base.X().Code), sadece metni değiştiriyoruz.
public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    // ---------- Kullanıcı adı / e-posta ----------
    public override IdentityError DuplicateUserName(string userName) =>
        new()
        {
            Code = base.DuplicateUserName(userName).Code,
            Description = $"'{userName}' kullanıcı adı zaten alınmış."
        };

    public override IdentityError DuplicateEmail(string email) =>
        new()
        {
            Code = base.DuplicateEmail(email).Code,
            Description = $"'{email}' adresiyle kayıtlı bir hesap zaten var."
        };

    public override IdentityError InvalidUserName(string? userName) =>
        new()
        {
            Code = base.InvalidUserName(userName).Code,
            Description = $"'{userName}' geçersiz bir kullanıcı adı. Sadece harf, rakam ve - . _ kullanılabilir (boşluk olmaz)."
        };

    public override IdentityError InvalidEmail(string? email) =>
        new()
        {
            Code = base.InvalidEmail(email).Code,
            Description = $"'{email}' geçerli bir e-posta adresi değil."
        };

    // ---------- Şifre kuralları (1.1'de belirlediklerimiz) ----------
    public override IdentityError PasswordTooShort(int length) =>
        new()
        {
            Code = base.PasswordTooShort(length).Code,
            Description = $"Şifre en az {length} karakter olmalıdır."
        };

    public override IdentityError PasswordRequiresDigit() =>
        new()
        {
            Code = base.PasswordRequiresDigit().Code,
            Description = "Şifre en az bir rakam (0-9) içermelidir."
        };

    public override IdentityError PasswordRequiresLower() =>
        new()
        {
            Code = base.PasswordRequiresLower().Code,
            Description = "Şifre en az bir küçük harf (a-z) içermelidir."
        };

    public override IdentityError PasswordRequiresUpper() =>
        new()
        {
            Code = base.PasswordRequiresUpper().Code,
            Description = "Şifre en az bir büyük harf (A-Z) içermelidir."
        };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new()
        {
            Code = base.PasswordRequiresNonAlphanumeric().Code,
            Description = "Şifre en az bir sembol (!, ?, * vb.) içermelidir."
        };

    public override IdentityError PasswordMismatch() =>
        new()
        {
            Code = base.PasswordMismatch().Code,
            Description = "Şifre hatalı."
        };

    // ---------- Onay linki ----------
    public override IdentityError InvalidToken() =>
        new()
        {
            Code = base.InvalidToken().Code,
            Description = "Bağlantı geçersiz veya süresi dolmuş."
        };

    // ---------- Roller (Görev 2'de kullanacağız) ----------
    public override IdentityError DuplicateRoleName(string role) =>
        new()
        {
            Code = base.DuplicateRoleName(role).Code,
            Description = $"'{role}' rolü zaten mevcut."
        };

    public override IdentityError UserAlreadyInRole(string role) =>
        new()
        {
            Code = base.UserAlreadyInRole(role).Code,
            Description = $"Kullanıcı zaten '{role}' rolünde."
        };

    public override IdentityError UserNotInRole(string role) =>
        new()
        {
            Code = base.UserNotInRole(role).Code,
            Description = $"Kullanıcı '{role}' rolünde değil."
        };

    // ---------- Genel ----------
    public override IdentityError DefaultError() =>
        new()
        {
            Code = base.DefaultError().Code,
            Description = "Beklenmeyen bir hata oluştu."
        };

    public override IdentityError ConcurrencyFailure() =>
        new()
        {
            Code = base.ConcurrencyFailure().Code,
            Description = "Kayıt başka biri tarafından değiştirildi, lütfen tekrar deneyin."
        };
}