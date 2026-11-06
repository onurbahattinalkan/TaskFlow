using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// [Authorize]: giriş yapmamış kullanıcı bu controller'ın hiçbir sayfasına giremez → Login'e yönlenir
[Authorize]
public class ProjectsController : Controller
{
    // Veritabanı bağlantımız (Program.cs'te zaten kayıtlı, otomatik gelir)
    private readonly AppDbContext _context;

    // "Bu kişi bu ekipte ne yapabilir?" sorularının cevabı
    private readonly ITeamAuthorizationService _teamAuth;

    public ProjectsController(AppDbContext context, ITeamAuthorizationService teamAuth)
    {
        _context = context;
        _teamAuth = teamAuth;
    }

    // GET: /Projects  →  Üstte proje kartları, altta yeni proje formu
    public async Task<IActionResult> Index()
    {
        // Form boş bir projeyle başlar
        var model = await BuildIndexModelAsync(new Project());
        return View(model);
    }

    // POST: /Projects/Create  →  Sayfanın altındaki formdan gelir
    // Prefix = "NewProject": formdaki alan adları "NewProject.Name" gibi olduğu için
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,StartDate,EndDate,TeamId", Prefix = "NewProject")] Project project)
    {
        // Tarih kuralı (hata anahtarı da "NewProject." ön ekiyle eklenmeli)
        ValidateDates(project, "NewProject.");

        // Seçilen ekibin lideri (ya da Admin) değilse proje açamaz.
        // Açılır listede zaten sadece kendi ekipleri var ama form elle değiştirilebilir.
        if (project.TeamId != 0 && !await _teamAuth.IsLeaderAsync(User, project.TeamId))
        {
            ModelState.AddModelError("NewProject.TeamId",
                "Sadece lideri olduğunuz ekiplere proje ekleyebilirsiniz.");
        }

        // Hata varsa aynı sayfayı (kartlar + hatalı form) tekrar göster
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexModelAsync(project));
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Sayfa yenilenince üstte yeşil mesaj (_TempDataAlerts)
        TempData["Success"] = $"\"{project.Name}\" projesi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Projects/Edit/5  →  Formu mevcut bilgilerle dolu göster
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        // Sadece projenin ekibinin lideri (ya da Admin) düzenleyebilir.
        // Forbid() → AccessDenied sayfası
        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        await LoadLeaderTeamsAsync(project.TeamId);
        return View(project);
    }

    // POST: /Projects/Edit/5  →  Değişiklikleri kaydet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Name,Description,StartDate,EndDate,TeamId")] Project project)
    {
        // Adresteki id ile formdaki gizli Id aynı olmalı
        if (id != project.Id)
            return NotFound();

        // Projenin veritabanındaki ŞU ANKİ ekibi (formdan gelen değişmiş olabilir).
        // Select ile sadece TeamId'yi çekiyoruz; int? sayesinde proje yoksa null gelir.
        var originalTeamId = await _context.Projects
            .Where(p => p.Id == id)
            .Select(p => (int?)p.TeamId)
            .FirstOrDefaultAsync();

        if (originalTeamId == null)
            return NotFound();

        // Mevcut ekibin lideri değilse düzenleyemez
        if (!await _teamAuth.IsLeaderAsync(User, originalTeamId.Value))
            return Forbid();

        // Proje başka bir ekibe taşınıyorsa o ekibin de lideri olmalı
        if (project.TeamId != originalTeamId && !await _teamAuth.IsLeaderAsync(User, project.TeamId))
        {
            ModelState.AddModelError(nameof(Project.TeamId),
                "Projeyi sadece lideri olduğunuz bir ekibe taşıyabilirsiniz.");
        }

        ValidateDates(project);

        if (!ModelState.IsValid)
        {
            await LoadLeaderTeamsAsync(project.TeamId);
            return View(project);
        }

        try
        {
            // Projeyi "değişti" olarak işaretle ve kaydet
            _context.Update(project);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Biz düzenlerken proje başka biri tarafından silindiyse → 404
            if (!await _context.Projects.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }

        TempData["Success"] = "Proje güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Projects/Delete/5  →  Silme onay sayfası
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        // Projeyi ekibiyle birlikte bul (onay sayfasında ekip adı görünsün)
        var project = await _context.Projects
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // Bu projeye ait kaç görev var? (uyarı mesajı için)
        ViewBag.TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);

        return View(project);
    }

    // POST: /Projects/Delete/5  →  Projeyi gerçekten sil
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project != null)
        {
            // POST'ta da yetki kontrolü (GET'i atlayıp doğrudan POST atılabilir)
            if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
                return Forbid();

            // Görevler, atamalar, yorumlar ve kategoriler veritabanında kademeli silinir
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"\"{project.Name}\" projesi silindi.";
        }

        return RedirectToAction(nameof(Index));
    }


    // ---------- Yardımcı metotlar ----------

    // Projeler sayfasının modelini hazırlar (Index GET ve hatalı Create POST kullanır)
    private async Task<ProjectsIndexViewModel> BuildIndexModelAsync(Project newProject)
    {
        // Görebileceği ekipler (üyesi olduğu; Admin'se hepsi)
        var memberTeamIds = await _teamAuth.GetMemberTeamIdsAsync(User);

        // Yönetebileceği ekipler (lideri olduğu; Admin'se hepsi)
        var leaderTeamIds = await _teamAuth.GetLeaderTeamIdsAsync(User);

        // Kart verileri. Select ile sadece gereken alanları çekiyoruz;
        // sayılar (Count) SQL tarafında hesaplanır, görevler belleğe yüklenmez.
        var cards = await _context.Projects
            .Where(p => memberTeamIds.Contains(p.TeamId))
            .OrderBy(p => p.Name)
            .Select(p => new ProjectCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                TeamId = p.TeamId,
                TeamName = p.Team!.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                TaskCount = p.Tasks.Count(),
                AssignmentCount = p.Tasks.SelectMany(t => t.Assignments).Count(),
                DoneCount = p.Tasks.SelectMany(t => t.Assignments)
                                   .Count(a => a.Status == TaskItemStatus.Done)
            })
            .ToListAsync();

        // Her kart için "düzenle/sil butonları görünsün mü?"
        foreach (var card in cards)
        {
            card.CanManage = leaderTeamIds.Contains(card.TeamId);
        }

        // Formdaki ekip listesi: sadece lideri olunan ekipler
        var leaderTeams = await _context.Teams
            .Where(t => leaderTeamIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .ToListAsync();

        return new ProjectsIndexViewModel
        {
            Projects = cards,
            NewProject = newProject,
            // TeamId 0 ise (henüz seçilmemiş) hiçbir ekip seçili gelmesin
            LeaderTeams = new SelectList(leaderTeams, "Id", "Name",
                newProject.TeamId == 0 ? null : newProject.TeamId)
        };
    }

    // Bitiş tarihi başlangıçtan önce olamaz.
    // prefix: Index formunda alanlar "NewProject.EndDate" olduğu için hata anahtarı da öyle olmalı
    private void ValidateDates(Project project, string prefix = "")
    {
        if (project.StartDate.HasValue && project.EndDate.HasValue
            && project.EndDate < project.StartDate)
        {
            ModelState.AddModelError(prefix + nameof(Project.EndDate),
                "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }
    }

    // Edit sayfasındaki ekip açılır listesi: sadece lideri olunan ekipler
    private async Task LoadLeaderTeamsAsync(int? selectedTeamId = null)
    {
        var leaderTeamIds = await _teamAuth.GetLeaderTeamIdsAsync(User);

        ViewBag.Teams = new SelectList(
            await _context.Teams
                .Where(t => leaderTeamIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ToListAsync(),
            "Id", "Name", selectedTeamId);
    }
}