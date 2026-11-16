namespace TaskFlow.ViewModels;

// Yönetici paneli ana sayfasının ihtiyaç duyduğu tüm veriler.
// Controller bu nesneyi doldurur, view sadece ekrana basar.
public class AdminDashboardViewModel
{
    // ---------- Üstteki özet kutucukları ----------
    public int UserCount {get; set;}     // toplam kullanıcı
    public int TeamCount {get; set;}     // toplam ekip
    public int ProjectCount {get; set;}  // toplam proje
    public int TaskCount {get; set;}     // toplam görev

    // Bitmemiş ve son tarihi geçmiş ATAMA sayısı.
    // Not: Bir görev 3 kişiye atandıysa 3 ayrı atama vardır; her biri ayrı sayılır.
    // (Onur'un proje dashboard'undaki "Geciken" ile aynı hesap.)
    public int OverdueCount {get; set;}

    // ---------- Alttaki kullanıcı tablosu ----------
    public List<UserListItemViewModel> Users {get; set;} = new();
}