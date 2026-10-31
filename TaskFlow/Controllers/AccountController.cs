using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// Kayıt, e-posta onayı, giriş ve çıkış işlemlerinin hepsi bu controller'da olacak.
// Adresler: /Account/Register, /Account/ConfirmEmail, /Account/Login ...
public class AccountController : Controller
{
    // UserManager: kullanıcı oluşturma, şifre kontrolü, token üretme gibi işler (Identity'nin hazır servisi)
    private readonly UserManager<ApplicationUser> _userManager;
    // 1.4'te yazdığımız e-posta servisi

    private readonly IEmailService _emailService;
    // SignInManager: şifre kontrolü + giriş çerezi verme / silme (oturum yönetimi)
    private readonly SignInManager<ApplicationUser> _signInManager;

    // Parametreleri ASP.NET otomatik verir (Dependency Injection).
    // UserManager'ı 1.1'deki AddIdentity satırı kaydetti, IEmailService'i 1.4'te biz kaydettik.
    public AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
    }

    // ------------------------------------------------------------------
    // GET /Account/Register  → boş kayıt formunu göster
    // ------------------------------------------------------------------
    [HttpGet]
    public IActionResult Register()
    {
        // Zaten giriş yapmış biri kayıt sayfasına gelirse ana sayfaya gönder
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    // ------------------------------------------------------------------
    // POST /Account/Register  → formdan gelen veriyle kullanıcı oluştur
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken] // CSRF koruması: form gerçekten bizim sayfamızdan mı geldi?
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // 1) ViewModel'deki kurallar (Required, EmailAddress, Compare...) geçti mi?
        //    Geçmediyse formu, girilen değerler ve hata mesajlarıyla geri göster.
        if (!ModelState.IsValid)
            return View(model);

        // 2) ViewModel'den veritabanı nesnesini oluştur.
        //    Sadece izin verdiğimiz alanları kopyalıyoruz (EmailConfirmed vb. dokunulmaz → false kalır).
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName
        };

        // 3) Identity kullanıcıyı oluştursun: şifreyi hash'ler, kuralları (rakam, küçük harf),
        //    kullanıcı adı/e-posta benzersizliğini kontrol eder, veritabanına yazar.
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            // Identity'nin hatalarını forma ekle (ör. "Bu kullanıcı adı alınmış").
            // string.Empty: hata belirli bir alana değil, formun geneline ait.
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // 4) Onay mailini gönder
        await SendConfirmationEmailAsync(user);

        // 5) "Mailinizi kontrol edin" sayfasına yönlendir.
        //    POST'tan sonra yönlendirme yapıyoruz (PRG deseni): kullanıcı F5'e basarsa
        //    form tekrar gönderilmez, ikinci bir kayıt denemesi olmaz.
        return RedirectToAction(nameof(RegisterConfirmation), new { email = user.Email });
    }

    // ------------------------------------------------------------------
    // GET /Account/RegisterConfirmation?email=...  → "Mailinizi kontrol edin" sayfası
    // ------------------------------------------------------------------
    [HttpGet]
    public IActionResult RegisterConfirmation(string email)
    {
        // ViewBag: controller'dan view'a küçük veri taşımanın kolay yolu
        ViewBag.Email = email;
        return View();
    }

    // ------------------------------------------------------------------
    // GET /Account/ConfirmEmail?userId=...&code=...  → mailteki linke tıklanınca çalışır
    // (Sayfasını 2.4'te yapacağız)
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
    {
        // Link eksik/bozuksa ana sayfaya gönder
        if (userId == null || code == null)
            return RedirectToAction("Index", "Home");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        // Linke koyarken Base64Url'e çevirmiştik, şimdi orijinal token'a geri çeviriyoruz.
        // Link elle bozulduysa çözme işlemi exception fırlatır → geçersiz bağlantı say.
        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            ViewBag.Success = false;
            return View();
        }

        // Identity token'ı doğrular; doğruysa EmailConfirmed = true yapıp kaydeder
        var result = await _userManager.ConfirmEmailAsync(user, token);

        ViewBag.Success = result.Succeeded;
        return View();
    }


    // ------------------------------------------------------------------
    // GET /Account/Login?ReturnUrl=...  → giriş formunu göster
    // ------------------------------------------------------------------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        // Girişten sonra dönülecek adresi view'a taşı (formda gizli olarak geri gönderilecek)
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ------------------------------------------------------------------
    // POST /Account/Login  → e-posta + şifre kontrolü, başarılıysa çerez ver
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // 1) Girişi e-posta ile yapıyoruz ama Identity kullanıcıyı UserName ile tanır.
        //    Bu yüzden önce e-postadan kullanıcıyı buluyoruz.
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Bilerek "e-posta kayıtlı değil" DEMİYORUZ (user enumeration'a karşı)
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        // 2) Şifre kontrolü + giriş.
        //    lockoutOnFailure: true → yanlış denemeler sayılır, 5'te hesap kilitlenir (1.1'deki ayar)
        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // 3) Sadece KENDİ sitemizin içindeki bir adrese dön (open redirect'e karşı)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsNotAllowed)
        {
            // RequireConfirmedEmail = true olduğu için onaysız hesap buraya düşer
            ModelState.AddModelError(string.Empty,
                "E-posta adresiniz henüz onaylanmamış. Lütfen mailinizdeki bağlantıya tıklayın.");
            return View(model);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Çok fazla hatalı deneme yapıldı. Hesabınız 5 dakika kilitlendi.");
            return View(model);
        }

        // Şifre yanlış
        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
        return View(model);
    }

    // ------------------------------------------------------------------
    // POST /Account/Logout  → giriş çerezini sil
    // GET değil POST: başka bir sitenin seni habersizce çıkış yaptırmasını
    // (CSRF) ve tarayıcının linki önceden yükleyip çıkış yaptırmasını engeller.
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    // ------------------------------------------------------------------
    // Yardımcı metot: onay linkini üretip mail atar.
    // private: dışarıdan adresle çağrılamaz, sadece bu controller kullanır.
    // ------------------------------------------------------------------
    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        // a) Kullanıcıya özel, imzalı onay kodu
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // b) URL'yi bozan karakterler (+ / =) olmasın diye Base64Url'e çevir
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // c) Tam link: http://localhost:5002/Account/ConfirmEmail?userId=...&code=...
        //    protocol verince "http://localhost:5002" kısmı da eklenir (mailde tam adres lazım)
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = user.Id, code }, protocol: Request.Scheme)!;

        // d) Mail içeriği. HtmlEncoder: linkteki özel karakterler HTML'i bozmasın
        var body = $@"
            <h3>Merhaba {HtmlEncoder.Default.Encode(user.FullName)},</h3>
            <p>TaskFlow hesabınızı etkinleştirmek için aşağıdaki bağlantıya tıklayın:</p>
            <p><a href=""{HtmlEncoder.Default.Encode(link)}"">E-posta adresimi onayla</a></p>
            <p>Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>";

        await _emailService.SendAsync(user.Email!, "TaskFlow - E-posta adresinizi onaylayın", body);
    }
}