using Microsoft.AspNetCore.Identity;
using TaskFlow.Models;

namespace TaskFlow.Data;

// Uygulama açılırken bir kez çalışır: rolleri ve admin hesabını oluşturur.
// Her adım önce "zaten var mı?" diye bakar; bu yüzden kaç kez çalışırsa çalışsın tekrar kayıt oluşmaz.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Gerekli servisleri DI'dan alıyoruz (controller'daki constructor'ın yaptığı işi burada elle yapıyoruz)
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // ---------- 1) Roller ----------
        foreach (var roleName in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Rol oluşturuldu: {Role}", roleName);
            }
        }

        // ---------- 1b) Eski hesaplar: hiç rolü olmayanlara "User" ver ----------
        // 3.2'den önce açılan hesapların rolü yok; onları yeni kurala ("herkes User") uyduruyoruz.
        // ToList(): önce tüm kullanıcıları belleğe al, sonra tek tek işle
        // (döngü içinde veritabanına yazarken açık sorgu kalmasın).
        var allUsers = userManager.Users.ToList();
        foreach (var u in allUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            if (roles.Count == 0)
            {
                await userManager.AddToRoleAsync(u, AppRoles.User);
                logger.LogInformation("{UserName} kullanıcısına User rolü verildi.", u.UserName);
            }
        }

        // ---------- 2) Admin hesabı ----------
        // Bilgiler user-secrets'taki "AdminUser" bölümünden okunur (koda şifre yazılmaz)
        var email = config["AdminUser:Email"];
        var userName = config["AdminUser:UserName"];
        var fullName = config["AdminUser:FullName"];
        var password = config["AdminUser:Password"];

        // Ayar girilmemişse uygulamayı çökertme; uyar ve admin oluşturmayı atla
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminUser ayarları eksik, admin hesabı oluşturulmadı. (user-secrets'ı kontrol edin)");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                Email = email,
                UserName = userName,
                FullName = fullName ?? "Sistem Yöneticisi",
                // Seed ile oluşan hesaba onay maili göndermiyoruz, doğrudan onaylı başlatıyoruz
                EmailConfirmed = true
            };

            // Şifre kuralları (rakam, küçük harf...) burada da geçerli
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                // Örn. şifre kurallara uymuyorsa nedenini konsola yaz
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                logger.LogError("Admin hesabı oluşturulamadı: {Errors}", errors);
                return;
            }
            logger.LogInformation("Admin hesabı oluşturuldu: {Email}", email);
        }

        // ---------- 3) Admin'i Admin rolüne ekle ----------
        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            logger.LogInformation("{Email} Admin rolüne eklendi.", email);
        }
    }
}