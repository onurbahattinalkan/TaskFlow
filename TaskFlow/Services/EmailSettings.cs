namespace TaskFlow.Services;

// appsettings.json'daki "EmailSettings" bölümünün C# karşılığı.
// Özellik adları JSON'daki anahtarlarla AYNI olmalı, eşleştirme isimle yapılır.
public class EmailSettings
{
	public string Host { get; set; } = "smtp.gmail.com"; // SMTP sunucusu
	public int Port { get; set; } = 587;                 // STARTTLS portu
	public string UserName { get; set; } = string.Empty; // gönderen Gmail adresi
	public string Password { get; set; } = string.Empty; // 16 haneli uygulama şifresi
	public string FromName { get; set; } = "TaskFlow";   // gönderen adı
}