namespace TaskFlow.Services;

// "Mail gönderebilen her sınıf bu metoda sahip olmalı" sözleşmesi.
// Controller'lar SmtpEmailService'i değil, bu arayüzü isteyecek.
public interface IEmailService
{
    // to: alıcı adresi, subject: konu, htmlBody: HTML içerik (onay linki için)
    Task SendAsync(string to, string subject, string htmlBody);
}