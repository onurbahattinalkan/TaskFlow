using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using Microsoft.AspNetCore.Identity;   // AddIdentity, IdentityRole için
using TaskFlow.Models;                 // ApplicationUser için
using TaskFlow.Services;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ASP.NET Identity: kullanıcı + rol sistemini açıyoruz.
// AddDefaultIdentity yerine AddIdentity kullanıyoruz çünkü:
//  - AddDefaultIdentity hazır İngilizce sayfaları getirir, biz kendimiz yazacağız
//  - AddIdentity rol desteğini (IdentityRole) de birlikte açar
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // E-postasını onaylamamış kullanıcı giriş yapamaz
        options.SignIn.RequireConfirmedEmail = true;

        // Aynı e-posta ile ikinci hesap açılamaz (giriş e-posta ile olacak)
        options.User.RequireUniqueEmail = true;

        // Kullanıcı adında izin verilen karakterler.
        // Varsayılan listede Türkçe harf yok; "çağrı" gibi adlar reddedilirdi.
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "çğıöşüÇĞİÖŞÜ0123456789-._";

        // Şifre kuralları
        options.Password.RequiredLength = 6;              // en az 6 karakter
        options.Password.RequireDigit = true;             // en az 1 rakam
        options.Password.RequireLowercase = true;         // en az 1 küçük harf
        options.Password.RequireUppercase = false;        // büyük harf şart değil
        options.Password.RequireNonAlphanumeric = false;  // sembol şart değil

        // 5 yanlış şifreden sonra hesap 5 dakika kilitlenir (kaba kuvvet saldırısına karşı)
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    // Kullanıcıları ve rolleri AppDbContext üzerinden PostgreSQL'de sakla
    .AddEntityFrameworkStores<AppDbContext>()
    // E-posta onay linki ve şifre sıfırlama için tek kullanımlık kod (token) üreticileri
    .AddDefaultTokenProviders();

// Giriş çerezi (cookie) ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    // Giriş yapmamış biri [Authorize] olan bir sayfaya girerse buraya yönlendirilir
    options.LoginPath = "/Account/Login";

    // Giriş yapmış ama yetkisi olmayan biri buraya yönlendirilir
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Çerez 7 gün geçerli kalır
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    // Kullanıcı siteyi kullandıkça bu 7 gün yeniden başlar
    // (aktif kullanıcıyı sürekli çıkış yaptırmamak için)
    options.SlidingExpiration = true;
});

// "EmailSettings" bölümünü EmailSettings sınıfına bağla
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Biri IEmailService isterse ona SmtpEmailService ver.
// AddTransient: her istendiğinde yeni bir nesne oluşturulur (servis durumsuz olduğu için uygun)
builder.Services.AddTransient<IEmailService, SmtpEmailService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ÖNCE kimlik doğrulama: "Bu istek kimden geliyor?" (tarayıcıdaki giriş çerezini okur)
app.UseAuthentication(); // kimsin ?
app.UseAuthorization(); // yetkin var mı ?

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
