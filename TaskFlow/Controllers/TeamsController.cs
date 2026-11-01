using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.ViewModels;
using TaskFlow.Services;

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

    // 3.5'te yazdığımız ekip yetki servisi
    private readonly ITeamAuthorizationService _teamAuth;

    public TeamsController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        ITeamAuthorizationService teamAuth)
    {
        _context = context;
        _userManager = userManager;
        _teamAuth = teamAuth;
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

    // ------------------------------------------------------------------
    // GET /Teams/Details/5  → ekip bilgisi + üyeler (sadece üyeler görebilir)
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // 1) Ekibi ve üyelerini tek sorguda, sadece gereken alanlarla çek
        var model = await _context.Teams
            .Where(t => t.Id == id)
            .Select(t => new TeamDetailsViewModel
            {
                TeamId = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Members = t.Members
                    // Lider en üstte, sonra kullanıcı adına göre
                    .OrderByDescending(m => m.TeamRole == TeamRoles.Leader)
                    .ThenBy(m => m.User.UserName)
                    .Select(m => new TeamMemberItemViewModel
                    {
                        UserId = m.UserId,
                        UserName = m.User.UserName ?? "",
                        FullName = m.User.FullName,
                        TeamRole = m.TeamRole,
                        JoinedAt = m.JoinedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        // 2) Ekip yoksa 404
        if (model == null)
            return NotFound();

        // 3) Üye değilse 403 → AccessDenied sayfası.
        //    URL'deki id'yi değiştirip başkasının ekibine bakmayı (IDOR) burası engeller.
        if (!await _teamAuth.IsMemberAsync(User, id))
            return Forbid();

        // 4) Lider mi? (view'da "Üye ekle" formunu göstermek için)
        model.CanManage = await _teamAuth.IsLeaderAsync(User, id);

        return View(model);
    }

    // ------------------------------------------------------------------
    // POST /Teams/AddMember  → ekibe üye ekle (sadece lider)
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(AddMemberViewModel model)
    {
        // 1) SUNUCU tarafı yetki kontrolü: form sadece lidere gösteriliyor
        //    ama biri isteği elle gönderebilir, o yüzden burada tekrar soruyoruz.
        if (!await _teamAuth.IsLeaderAsync(User, model.TeamId))
            return Forbid();

        // Form Details sayfasının içinde. Hata olursa o sayfaya dönüp mesajı TempData ile taşıyoruz.
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Kullanıcı adı veya e-posta girin.";
            return RedirectToAction(nameof(Details), new { id = model.TeamId });
        }

        // 2) Kullanıcıyı bul: içinde @ varsa e-posta, yoksa kullanıcı adı olarak ara
        var input = model.UserNameOrEmail.Trim();
        var userToAdd = input.Contains('@')
            ? await _userManager.FindByEmailAsync(input)
            : await _userManager.FindByNameAsync(input);

        if (userToAdd == null)
        {
            TempData["Error"] = $"'{input}' adında/adresinde bir kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Details), new { id = model.TeamId });
        }

        // 3) Zaten üye mi?
        var alreadyMember = await _context.TeamMembers
            .AnyAsync(m => m.TeamId == model.TeamId && m.UserId == userToAdd.Id);
        if (alreadyMember)
        {
            TempData["Error"] = $"{userToAdd.UserName} zaten bu ekibin üyesi.";
            return RedirectToAction(nameof(Details), new { id = model.TeamId });
        }

        // 4) Üye olarak ekle
        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = model.TeamId,
            UserId = userToAdd.Id,
            TeamRole = TeamRoles.Member
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{userToAdd.UserName} ekibe üye olarak eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.TeamId });
    }

    // ------------------------------------------------------------------
    // POST /Teams/TransferLeadership  → liderliği başka bir üyeye devret
    // ------------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferLeadership(int teamId, string newLeaderUserId)
    {
        // 1) Sadece mevcut lider (veya sistem Admin'i) devredebilir
        if (!await _teamAuth.IsLeaderAsync(User, teamId))
            return Forbid();

        // 2) Hedef kişi bu ekibin üyesi mi? (ekip dışından birine liderlik verilemez)
        var newLeader = await _context.TeamMembers
            .Include(m => m.User) // mesajda kullanıcı adını göstermek için
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == newLeaderUserId);

        if (newLeader == null)
        {
            TempData["Error"] = "Seçilen kişi bu ekibin üyesi değil.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // 3) Zaten lider mi?
        if (newLeader.TeamRole == TeamRoles.Leader)
        {
            TempData["Error"] = $"{newLeader.User.UserName} zaten ekibin lideri.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // 4) Ekipte şu an Lider olan kayıt(lar)ı bul ve Üye yap.
        //    İsteği yapan kişiyi değil "ekipteki lideri" arıyoruz: Admin devrettiğinde de doğru çalışsın.
        var currentLeaders = await _context.TeamMembers
            .Where(m => m.TeamId == teamId && m.TeamRole == TeamRoles.Leader)
            .ToListAsync();

        foreach (var leader in currentLeaders)
            leader.TeamRole = TeamRoles.Member;

        // 5) Yeni lideri ata
        newLeader.TeamRole = TeamRoles.Leader;

        // 6) TEK SaveChanges = TEK transaction:
        //    "lidersiz ekip" ya da "iki liderli ekip" ara durumu asla kalıcı olmaz.
        //    (EF, ToListAsync ile çektiğimiz nesnelerdeki değişiklikleri kendisi takip eder,
        //     ayrıca Update çağırmamıza gerek yok.)
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Liderlik {newLeader.User.UserName} kullanıcısına devredildi.";
        return RedirectToAction(nameof(Details), new { id = teamId });
    }
}

