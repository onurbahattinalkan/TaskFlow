namespace TaskFlow.Helpers;

// Tarih/saat yardımcıları.
// Kural: veritabanında HER ZAMAN UTC saklarız, ekranda Türkiye saatine çeviririz.
public static class TimeHelper
{
    // Türkiye saat dilimi (.NET 6+ Windows'ta da bu IANA adını tanır)
    private static readonly TimeZoneInfo Turkey =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    // UTC → Türkiye saati (ekranda göstermek için)
    public static DateTime ToTurkey(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Turkey);

    // Türkiye saati → UTC (formdan gelen saati kaydetmeden önce; 4. aşamada kullanacağız)
    public static DateTime ToUtc(DateTime turkeyTime) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(turkeyTime, DateTimeKind.Unspecified), Turkey);

    // "06.11.2026 14:30" biçiminde Türkiye saati
    public static string Format(DateTime utc) => ToTurkey(utc).ToString("dd.MM.yyyy HH:mm");

    // Deadline'a kalan süre metni + Bootstrap renk sınıfı
    //   24 saatten fazla → "2 gün 5 saat kaldı"  (yeşil)
    //   24 saatten az    → "3 saat 10 dk kaldı"  (turuncu)
    //   geçmiş           → "1 gün 2 saat gecikti" (kırmızı)
    public static (string Text, string CssClass) Remaining(DateTime dueUtc)
    {
        var diff = dueUtc - DateTime.UtcNow;   // pozitif: süre var, negatif: gecikmiş
        var abs = diff.Duration();             // işaretsiz (mutlak) süre

        // Sürenin okunur hali
        string amount;
        if (abs.TotalDays >= 1)
            amount = $"{(int)abs.TotalDays} gün {abs.Hours} saat";
        else if (abs.TotalHours >= 1)
            amount = $"{(int)abs.TotalHours} saat {abs.Minutes} dk";
        else
            amount = $"{Math.Max(abs.Minutes, 1)} dk";

        if (diff < TimeSpan.Zero)
            return ($"{amount} gecikti", "text-danger");

        if (diff < TimeSpan.FromHours(24))
            return ($"{amount} kaldı", "text-warning");

        return ($"{amount} kaldı", "text-success");
    }
}