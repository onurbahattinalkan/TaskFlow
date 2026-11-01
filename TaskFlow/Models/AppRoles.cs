namespace TaskFlow.Models;

// Sistem rollerinin adları. Kodun her yerinde "Admin" yazmak yerine AppRoles.Admin kullanıyoruz:
// yazım hatası yapılırsa derleyici yakalar, rol adı değişirse tek yerden değişir.
public static class AppRoles
{
    public const string Admin = "Admin"; // site yöneticisi: tüm kullanıcıları/ekipleri görür
    public const string User = "User";   // kayıt olan herkes
}