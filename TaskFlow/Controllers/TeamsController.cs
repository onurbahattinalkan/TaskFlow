using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// Ekip işlemleri: giriş yapmış herkes kullanabilir (sınıf seviyesinde [Authorize]).
// Lider'e özel işlemlerin (üye ekleme, devretme) kontrolünü metotların içinde yapacağız (3.4b).
[Authorize]
public class TeamsController : Controller
{
    // Ekip/üyelik tablolarına doğrudan erişim için veritabanı bağlamı
    private readonly AppDbContext _context;
    // Giriş yapan kullanıcının Id'sini almak için
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ------------------------------------------------------------------
    // GET /Teams  → giriş yapan kullanıcının üyesi olduğu ekipler
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Giriş çerezinden kullanıcının Id'si (veritabanına gitmeden)
        var userId = _userManager.GetUserId(User);

        // Üyelik tablosundan başla: "UserId'si benim olan satırlar"
        // Select ile sadece ihtiyacımız olan alanları çekiyoruz → EF tek bir SQL sorgusu üretir
        var teams = await _context.TeamMembers
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Team.Name)
            .Select(m => new TeamListItemViewModel
            {
                TeamId = m.TeamId,
                Name = m.Team.Name,
                Description = m.Team.Description,
                MyRole = m.TeamRole,
                MemberCount = m.Team.Members.Count
            })
            .ToListAsync();

        return View(teams);
    }

    // ------------------------------------------------------------------
    // GET /Teams/Create  → boş ekip formu
    // ------------------------------------------------------------------
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // ------------------------------------------------------------------
    // POST /Teams/Create  → ekibi oluştur, kuranı Lider yap
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeamCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;

        // 1) Ekip nesnesi
        var team = new Team
        {
            Name = model.Name,
            Description = model.Description
            // CreatedAt modelde varsayılan olarak DateTime.UtcNow
        };

        // 2) Kuran kişinin üyeliği: rolü LİDER.
        //    TeamId yerine Team nesnesini veriyoruz; ekibin Id'si henüz yok (veritabanı verecek),
        //    EF kaydederken önce ekibi ekler, oluşan Id'yi bu satıra otomatik yazar.
        var membership = new TeamMember
        {
            Team = team,
            UserId = userId,
            TeamRole = TeamRoles.Leader
        };

        _context.Teams.Add(team);
        _context.TeamMembers.Add(membership);

        // 3) TEK SaveChanges = TEK transaction: ya ikisi birden kaydedilir ya hiçbiri.
        //    Böylece "lideri olmayan ekip" oluşma ihtimali yok.
        await _context.SaveChangesAsync();

        // TempData: yönlendirmeden SONRAKİ sayfaya bir kerelik mesaj taşır
        TempData["Success"] = $"'{team.Name}' ekibi oluşturuldu. Ekibin lideri sizsiniz.";
        return RedirectToAction(nameof(Index));
    }
}