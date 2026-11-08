using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using TaskFlow.Helpers;

namespace TaskFlow.Controllers;

// Kategori yönetimi. Giriş yapmamış kullanıcı giremez.
[Authorize]
public class CategoriesController : Controller
{
    private readonly AppDbContext _context;

    // Onur'un yetki servisi: "bu kişi bu ekibin lideri mi?"
    private readonly ITeamAuthorizationService _teamAuth;

    public CategoriesController(AppDbContext context, ITeamAuthorizationService teamAuth)
    {
        _context = context;
        _teamAuth = teamAuth;
    }

    // GET: /Categories?projectId=1  →  Projenin kategori listesi
    public async Task<IActionResult> Index(int projectId)
    {
        // Projeyi ekibiyle birlikte bul
        var project = await _context.Projects
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return NotFound();

        // Sadece ekip Lideri ve Admin kategorileri yönetebilir
        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // Projenin kategorileri + her birinde kaç görev olduğu (COUNT SQL'de hesaplanır)
        var items = await _context.Categories
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItem
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                TaskCount = c.Tasks.Count()
            })
            .ToListAsync();

        SetProjectNav(project);

        return View(new CategoryIndexViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Items = items
        });
    }


    // GET: /Categories/Create?projectId=1  →  Boş form
    public async Task<IActionResult> Create(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        SetProjectNav(project);
        return View(new CategoryFormViewModel { ProjectId = projectId });
    }

    // POST: /Categories/Create  →  Kaydet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ProjectId,Name,Color")] CategoryFormViewModel form)
    {
        var project = await _context.Projects
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == form.ProjectId);

        if (project == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // Ad benzersizliği + renk kontrolü
        await ValidateFormAsync(form);

        if (!ModelState.IsValid)
        {
            SetProjectNav(project);
            return View(form);
        }

        _context.Categories.Add(new Category
        {
            Name = form.Name.Trim(),
            Color = form.Color,
            ProjectId = project.Id
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{form.Name.Trim()}\" kategorisi eklendi.";
        return RedirectToAction(nameof(Index), new { projectId = project.Id });
    }

    // GET: /Categories/Edit/5  →  Formu mevcut bilgilerle doldur
    public async Task<IActionResult> Edit(int id)
    {
        var category = await FindCategoryAsync(id);
        if (category == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, category.Project!.TeamId))
            return Forbid();

        SetProjectNav(category.Project);
        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            ProjectId = category.ProjectId,
            Name = category.Name,
            Color = category.Color
        });
    }

    // POST: /Categories/Edit/5  →  Değişiklikleri kaydet
    // ProjectId formdan alınmıyor (kategori başka projeye taşınamasın)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Color")] CategoryFormViewModel form)
    {
        if (id != form.Id)
            return NotFound();

        var category = await FindCategoryAsync(id);
        if (category == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, category.Project!.TeamId))
            return Forbid();

        // Proje kategorinin kendisinden alınır
        form.ProjectId = category.ProjectId;

        await ValidateFormAsync(form);

        if (!ModelState.IsValid)
        {
            SetProjectNav(category.Project);
            return View(form);
        }

        category.Name = form.Name.Trim();
        category.Color = form.Color;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{category.Name}\" kategorisi güncellendi.";
        return RedirectToAction(nameof(Index), new { projectId = category.ProjectId });
    }

    // GET: /Categories/Delete/5  →  Silme onay sayfası
    public async Task<IActionResult> Delete(int id)
    {
        var category = await FindCategoryAsync(id);
        if (category == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, category.Project!.TeamId))
            return Forbid();

        SetProjectNav(category.Project);
        return View(new CategoryDeleteViewModel
        {
            Id = category.Id,
            ProjectId = category.ProjectId,
            Name = category.Name,
            Color = category.Color,
            // Bu kategoride kaç görev var? (varsa silinemez)
            TaskCount = await _context.Tasks.CountAsync(t => t.CategoryId == id)
        });
    }

    // POST: /Categories/Delete/5  →  Gerçekten sil
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await FindCategoryAsync(id);
        if (category == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, category.Project!.TeamId))
            return Forbid();

        // Onay sayfası açıkken bu kategoriye görev eklenmiş olabilir → tekrar say
        var taskCount = await _context.Tasks.CountAsync(t => t.CategoryId == id);
        if (taskCount > 0)
        {
            TempData["Error"] = $"\"{category.Name}\" kategorisinde {taskCount} görev var. " +
                                "Önce bu görevleri başka bir kategoriye taşıyın.";
            return RedirectToAction(nameof(Index), new { projectId = category.ProjectId });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{category.Name}\" kategorisi silindi.";
        return RedirectToAction(nameof(Index), new { projectId = category.ProjectId });
    }

    // ---------- Yardımcı metot ----------

    // Sol menünün bilgisi (_ProjectLayout okur). "Kategoriler" sekmesi seçili.
    // Bu sayfalara sadece yönetici girebildiği için IsManager her zaman true.
    private void SetProjectNav(Project project)
    {
        ViewData["ProjectNav"] = new ProjectNavViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            TeamName = project.Team?.Name ?? "",
            IsManager = true,
            ActiveTab = ProjectTabs.Categories
        };
    }

    // Kategoriyi projesi ve ekibiyle birlikte bul (yetki kontrolü ve sol menü için)
    private Task<Category?> FindCategoryAsync(int id) =>
        _context.Categories
            .Include(c => c.Project).ThenInclude(p => p!.Team)
            .FirstOrDefaultAsync(c => c.Id == id);

    // Ekleme ve düzenlemenin ortak kontrolleri. Hataları ModelState'e ekler.
    private async Task ValidateFormAsync(CategoryFormViewModel form)
    {
        // Renk paletten olmalı (form elle değiştirilmiş olabilir)
        if (!CategoryColors.All.ContainsKey(form.Color))
        {
            ModelState.AddModelError(nameof(form.Color), "Paletten bir renk seçin.");
        }

        var name = form.Name?.Trim() ?? "";
        if (name.Length == 0)
            return;   // boş ad zaten [Required] hatası verdi

        // Aynı projede aynı isim var mı? (büyük/küçük harf duyarsız)
        // Düzenlemede kategorinin KENDİSİ hariç (c.Id != form.Id)
        var exists = await _context.Categories.AnyAsync(c =>
            c.ProjectId == form.ProjectId &&
            c.Id != form.Id &&
            c.Name.ToLower() == name.ToLower());

        if (exists)
        {
            ModelState.AddModelError(nameof(form.Name), "Bu projede aynı isimde bir kategori zaten var.");
        }
    }
}