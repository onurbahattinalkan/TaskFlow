namespace TaskFlow.Models;

//hata sayfasına veri taşıyan sınıftır. Sadece ekranda gösterilecek bilgiyi tutar. MVC projesi oluşturulurken şablonla hazır gelir.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
