namespace TaskFlow.Helpers;

// Kategori renk paleti. Kullanıcı sadece bu renklerden seçebilir
// (beyaz yazının okunabileceği koyu tonlar).
public static class CategoryColors
{
    public const string Default = "#0d6efd";

    // Renk kodu → Türkçe adı (renk kutusunun üzerine gelince görünür)
    public static readonly Dictionary<string, string> All = new()
    {
        ["#0d6efd"] = "Mavi",
        ["#6f42c1"] = "Mor",
        ["#d63384"] = "Pembe",
        ["#dc3545"] = "Kırmızı",
        ["#fd7e14"] = "Turuncu",
        ["#198754"] = "Yeşil",
        ["#20c997"] = "Turkuaz",
        ["#6c757d"] = "Gri"
    };
}