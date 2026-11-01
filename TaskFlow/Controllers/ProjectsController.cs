using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskFlow.Controllers;

public class ProjectsController : Controller
{
    // Veritabanı bağlantımız (Program.cs'te zaten kayıtlı, otomatik gelir)
    private readonly AppDbContext _context;

    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Projects  →  Proje listesi
    public async Task<IActionResult> Index()
    {
        var projects = await _context.Projects
            //projenin ekibini de dahil et
            .Include(p => p.Team)
            //isme göre sıralama
            .OrderBy(p => p.Name)
            //listeyi al
            .ToListAsync();

        // Listeyi view'a gönder
        return View(projects);
    }

        // GET: /Projects/Create  →  Boş formu göster
    public IActionResult Create()
    {
        // Açılır liste için ekipleri hazırla
        LoadTeams();
        return View();
    }

    // POST: /Projects/Create  →  Formu kaydet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,StartDate,EndDate,TeamId")] Project project)
    {
        // Tarih kuralını kontrol et (hata varsa ModelState'e eklenir)
        ValidateDates(project);

        // Doğrulama hatası varsa formu hatalarla birlikte geri göster
        if (!ModelState.IsValid)
        {
            // Açılır listeyi tekrar doldur, seçili ekip korunsun
            LoadTeams(project.TeamId);
            return View(project);
        }

        // Hata yoksa veritabanına ekle ve kaydet
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Listeye geri dön
        return RedirectToAction(nameof(Index));
    }


        // GET: /Projects/Edit/5  →  Formu mevcut bilgilerle dolu göster
    public async Task<IActionResult> Edit(int? id)
    {
        // Adreste id yoksa (/Projects/Edit) → 404
        if (id == null)
        {
            return NotFound();
        }

        // Projeyi id ile bul
        var project = await _context.Projects.FindAsync(id);

        // Böyle bir proje yoksa → 404
        if (project == null)
        {
            return NotFound();
        }

        // Açılır listeyi doldur, projenin mevcut ekibi seçili gelsin
        LoadTeams(project.TeamId);
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
        {
            return NotFound();
        }

        // Tarih kuralını kontrol et (hata varsa ModelState'e eklenir)
        ValidateDates(project);

        // Doğrulama hatası varsa formu hatalarla geri göster
        if (!ModelState.IsValid)
        {
            LoadTeams(project.TeamId);
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
            {
                return NotFound();
            }
            // Başka bir hata ise olduğu gibi fırlat
            throw;
        }

        // Listeye geri dön
        return RedirectToAction(nameof(Index));
    }

        // GET: /Projects/Delete/5  →  Silme onay sayfası
    public async Task<IActionResult> Delete(int? id)
    {
        // Adreste id yoksa → 404
        if (id == null)
        {
            return NotFound();
        }

        // Projeyi ekibiyle birlikte bul (onay sayfasında ekip adını göstermek için)
        var project = await _context.Projects
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == id);

        // Böyle bir proje yoksa → 404
        if (project == null)
        {
            return NotFound();
        }

        // Bu projeye ait kaç görev var? (uyarı mesajı için)
        ViewBag.TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);

        return View(project);
    }

    // POST: /Projects/Delete/5  →  Projeyi gerçekten sil
    // Metot adı farklı (DeleteConfirmed) ama adres yine /Projects/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Projeyi bul
        var project = await _context.Projects.FindAsync(id);

        // Varsa sil (görevleri ve yorumları veritabanı otomatik siler)
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        // Listeye geri dön
        return RedirectToAction(nameof(Index));
    }


    // Yardımcı metot: bitiş tarihi başlangıçtan önce olamaz
    // (Create ve Edit POST'ta çağrılır)
    private void ValidateDates(Project project)
    {
        // İki tarih de girilmişse ve bitiş başlangıçtan önceyse hata ekle
        if (project.StartDate.HasValue && project.EndDate.HasValue
            && project.EndDate < project.StartDate)
        {
            // Hata "Bitiş Tarihi" alanının altında görünsün
            ModelState.AddModelError(nameof(Project.EndDate),
                "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }
    }


    // Yardımcı metot: ekipleri açılır liste için ViewBag'e koyar
    // (Create ve ileride Edit sayfasında kullanılacak)
    private void LoadTeams(int? selectedTeamId = null)
    {
        ViewBag.Teams = new SelectList(
            _context.Teams.OrderBy(t => t.Name).ToList(), // ekipler, ada göre
            "Id",              // seçilince gönderilecek değer
            "Name",            // listede görünecek yazı
            selectedTeamId);   // varsa seçili gelecek ekip
    }

}