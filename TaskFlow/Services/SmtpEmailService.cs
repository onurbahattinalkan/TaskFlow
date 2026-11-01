using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TaskFlow.Services;

// IEmailService'in Gmail SMTP ile çalışan hali.
// SMTP: e-postaların sunucular arasında gönderildiği protokol.
// Uygulamamız Gmail sunucusuna bağlanır, kimliğini kanıtlar ve mesajı teslim eder.
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    // Constructor'daki parametreleri ASP.NET otomatik doldurur (Dependency Injection).
    // IOptions<EmailSettings>: Program.cs'te ayarlardan doldurulan EmailSettings nesnesi.
    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        // 1) Mesajı oluştur: kimden, konu, içerik
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.UserName, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true // içerikte <a href="..."> gibi HTML kullanacağız
        };
        message.To.Add(to); // kime

        // 2) Gmail sunucusuna bağlanacak istemciyi hazırla
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            // Bağlantıyı TLS ile şifrele: şifre ve içerik ağda açık metin gitmesin
            EnableSsl = true,
            // Gmail adresi + uygulama şifresi ile giriş
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
        };

        // 3) Gönder. "await": gönderim sürerken sunucu başka isteklere cevap verebilsin
        await client.SendMailAsync(message);

        // Konsolda görmek için kayıt düş
        _logger.LogInformation("E-posta gönderildi: {To} - {Subject}", to, subject);
    }
}