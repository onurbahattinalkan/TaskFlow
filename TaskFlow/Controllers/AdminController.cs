using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;        // YENİ: AppDbContext için
using TaskFlow.Models;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// Sınıfın başındaki [Authorize]: bu controller'daki TÜM metotlar sadece Admin rolüne açık.
//  - Giriş yapmamış biri  → Login sayfasına
//  - Giriş yapmış ama Admin değil → AccessDenied sayfasına
// Yeni metot eklendiğinde ayrıca korumaya gerek yok, otomatik korunur.
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    // YENİ: ekip/proje/görev sayılarını almak için veritabanı bağlamı
    private readonly AppDbContext _context;

    // ASP.NET, Program.cs'te kayıtlı servisleri buraya otomatik verir
    public AdminController(UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // ------------------------------------------------------------------
    // GET /Admin  → yönetici paneli ana sayfası (özet sayılar + kullanıcı listesi)
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Geciken hesabı için "şu an". Veritabanında tarihler UTC olduğu için UtcNow.
        var now = DateTime.UtcNow;

        // 1) Özet sayılar: her satır veritabanına tek bir COUNT sorgusu gönderir
        var model = new AdminDashboardViewModel
        {
            UserCount = await _userManager.Users.CountAsync(),
            TeamCount = await _context.Teams.CountAsync(),
            ProjectCount = await _context.Projects.CountAsync(),
            TaskCount = await _context.Tasks.CountAsync(),

            // Bitmemiş (Done değil) ve son tarihi şu andan önce olan atamalar
            OverdueCount = await _context.TaskAssignments
                .CountAsync(a => a.Status != TaskItemStatus.Done && a.DueDate < now)
        };

        // 2) Her kullanıcının kaç ekipte olduğu: TeamMembers'ı UserId'ye göre grupla.
        //    Tek sorgu → sonuç sözlük: { "kullanıcıId": ekipSayısı, ... }
        var teamCounts = await _context.TeamMembers
            .GroupBy(tm => tm.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        // 3) Kullanıcıları kullanıcı adına göre sıralı al
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        // 4) Her kullanıcıyı tablo satırına çevir.
        //    Roller ayrı tabloda olduğu için kullanıcı başına GetRolesAsync çağrılıyor
        //    (birkaç düzine kullanıcı için sorun değil).
        foreach (var user in users)
        {
            model.Users.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = await _userManager.GetRolesAsync(user),

                // Sözlükte yoksa (hiç ekibi yoksa) 0
                TeamCount = teamCounts.GetValueOrDefault(user.Id)
            });
        }

        return View(model);
    }

    // ------------------------------------------------------------------
    // GET /Admin/Users  → ESKİ liste sayfası. 4. adımda silinecek.
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                Roles = await _userManager.GetRolesAsync(user)
            });
        }

        return View(model);
    }
}