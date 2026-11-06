using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Helpers;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;

namespace TaskFlow.Controllers;

// Görev sayfaları. Giriş yapmamış kullanıcı giremez.
[Authorize]
public class TasksController : Controller
{
    private readonly AppDbContext _context;
    private readonly ITeamAuthorizationService _teamAuth;

    public TasksController(AppDbContext context, ITeamAuthorizationService teamAuth)
    {
        _context = context;
        _teamAuth = teamAuth;
    }

    // GET: /Tasks?projectId=1  →  Yönetici görev sayfası (form + gruplu liste)
    public async Task<IActionResult> Index(int projectId)
    {
        var project = await FindProjectAsync(projectId);
        if (project == null)
            return NotFound();

        // Ekipte değilse hiç göremez
        if (!await _teamAuth.IsMemberAsync(User, project.TeamId))
            return Forbid();

        // Üye ama lider değilse kendi panosuna (6. aşamada gelecek)
        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return RedirectToAction("My", new { projectId });

        // Boş formla sayfayı hazırla
        var model = await BuildManageModelAsync(project, new TaskCreateViewModel { ProjectId = project.Id });
        return View(model);
    }

    // POST: /Tasks/Create  →  Görevi oluştur ve seçilen kişilere ata
    // Prefix = "Form": formdaki alan adları "Form.Title" gibi
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] TaskCreateViewModel form)
    {
        var project = await FindProjectAsync(form.ProjectId);
        if (project == null)
            return NotFound();

        // Sadece Lider / Admin görev oluşturabilir
        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // ---------- 1) KATEGORİ ----------
        Category? category = null;
        var newName = form.NewCategoryName?.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            // Bu projede aynı isimde kategori var mı? (büyük/küçük harf duyarsız)
            category = await _context.Categories.FirstOrDefaultAsync(c =>
                c.ProjectId == project.Id && c.Name.ToLower() == newName.ToLower());

            // Yoksa yeni kategori nesnesi oluştur (görevle birlikte kaydedilecek)
            if (category == null)
            {
                // Renk paletten değilse (form elle değiştirildiyse) varsayılanı kullan
                var color = CategoryColors.All.ContainsKey(form.NewCategoryColor)
                    ? form.NewCategoryColor
                    : CategoryColors.Default;

                category = new Category { Name = newName, Color = color, ProjectId = project.Id };
            }
        }
        else if (form.CategoryId.HasValue)
        {
            // Seçilen kategori gerçekten bu projeye mi ait?
            category = await _context.Categories.FirstOrDefaultAsync(c =>
                c.Id == form.CategoryId && c.ProjectId == project.Id);
        }

        if (category == null)
        {
            ModelState.AddModelError("Form.CategoryId", "Bir kategori seçin ya da yeni kategori adı yazın.");
        }

        // ---------- 2) ATANAN KİŞİLER ----------
        // Projenin ekibindeki kullanıcıların Id'leri
        var memberIds = await _context.TeamMembers
            .Where(m => m.TeamId == project.TeamId)
            .Select(m => m.UserId)
            .ToListAsync();

        var selected = form.Assignees.Where(a => a.Selected).ToList();
        if (!selected.Any())
        {
            ModelState.AddModelError("Form.Assignees", "En az bir kişi seçmelisiniz.");
        }

        var nowUtc = DateTime.UtcNow;

        // Index'li döngü: hata mesajını doğru kişinin deadline kutusunun altına koymak için
        for (int i = 0; i < form.Assignees.Count; i++)
        {
            var a = form.Assignees[i];
            if (!a.Selected)
                continue;

            // Form elle değiştirilip ekip dışından biri gönderildiyse
            if (!memberIds.Contains(a.UserId))
            {
                ModelState.AddModelError("Form.Assignees", "Seçilen kişilerden biri bu projenin ekibinde değil.");
                continue;
            }

            if (a.DueDate == null)
            {
                ModelState.AddModelError($"Form.Assignees[{i}].DueDate", "Deadline seçin.");
            }
            else if (TimeHelper.ToUtc(a.DueDate.Value) <= nowUtc)
            {
                ModelState.AddModelError($"Form.Assignees[{i}].DueDate", "Deadline geçmiş bir zaman olamaz.");
            }
        }

        // Hata varsa aynı sayfayı, girilen bilgiler korunarak tekrar göster
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildManageModelAsync(project, form));
        }

        // ---------- 3) KAYDET ----------
        var task = new TaskItem
        {
            Title = form.Title.Trim(),
            Description = form.Description,
            ProjectId = project.Id,
            Category = category!,   // yeni kategoriyse EF onu da ekler
            CreatedAt = nowUtc
        };

        // Seçilen her kişi için bir atama satırı
        foreach (var a in selected)
        {
            task.Assignments.Add(new TaskAssignment
            {
                UserId = a.UserId,
                DueDate = TimeHelper.ToUtc(a.DueDate!.Value),  // Türkiye saati → UTC
                Status = TaskItemStatus.Todo,                  // "Atanan" sütununa düşer
                AssignedAt = nowUtc
            });
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();   // kategori + görev + atamalar tek seferde

        TempData["Success"] = $"\"{task.Title}\" görevi {selected.Count} kişiye atandı.";
        return RedirectToAction(nameof(Index), new { projectId = project.Id });
    }


    // ---------- Yardımcı metotlar ----------

    // Projeyi ekibiyle birlikte bul
    private Task<Project?> FindProjectAsync(int projectId) =>
        _context.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);

    // Yönetici sayfasının modelini hazırlar (GET ve hatalı POST kullanır)
    private async Task<TaskManageViewModel> BuildManageModelAsync(Project project, TaskCreateViewModel form)
    {
        // Ekip üyeleri (formdaki kişi listesi), kullanıcı adına göre
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == project.TeamId)
            .OrderBy(m => m.User.UserName)
            .Select(m => new { m.UserId, m.User.UserName, m.User.FullName })
            .ToListAsync();

        // Formdaki kişi listesini ekip üyelerinden kur.
        // Hatalı POST'tan dönülüyorsa önceki seçimler ve deadline'lar korunsun.
        form.Assignees = members.Select(m =>
        {
            var posted = form.Assignees.FirstOrDefault(x => x.UserId == m.UserId);
            return new AssigneeInput
            {
                UserId = m.UserId,
                UserName = m.UserName ?? "",
                FullName = m.FullName,
                Selected = posted?.Selected ?? false,
                DueDate = posted?.DueDate
            };
        }).ToList();

        // Projenin kategorileri
        var categories = await _context.Categories
            .Where(c => c.ProjectId == project.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Projenin tüm atamaları, listede gereken alanlarla (tek sorgu)
        var rows = await _context.TaskAssignments
            .Where(a => a.TaskItem!.ProjectId == project.Id)
            .Select(a => new
            {
                a.UserId,
                UserName = a.User!.UserName,
                FullName = a.User.FullName,
                Row = new TaskListRow
                {
                    TaskId = a.TaskItemId,
                    AssignmentId = a.Id,
                    Title = a.TaskItem!.Title,
                    Description = a.TaskItem.Description,
                    CategoryName = a.TaskItem.Category!.Name,
                    CategoryColor = a.TaskItem.Category.Color,
                    DueDate = a.DueDate,
                    AssignedAt = a.AssignedAt,
                    CreatedAt = a.TaskItem.CreatedAt,
                    Status = a.Status
                }
            })
            .ToListAsync();

        // Kişiye göre grupla. Grup içi varsayılan sıralama:
        // deadline'a göre artan, deadline eşitse önce oluşturulan üstte (8. aşamada filtreler eklenecek)
        var groups = rows
            .GroupBy(r => new { r.UserId, r.UserName, r.FullName })
            .Select(g => new TaskGroup
            {
                UserId = g.Key.UserId,
                UserName = g.Key.UserName ?? "",
                FullName = g.Key.FullName,
                Rows = g.Select(x => x.Row)
                        .OrderBy(r => r.DueDate)
                        .ThenBy(r => r.CreatedAt)
                        .ToList()
            })
            .OrderBy(g => g.UserName)
            .ToList();

        // Sol menü: "Görevler" sekmesi seçili
        ViewData["ProjectNav"] = new ProjectNavViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            TeamName = project.Team?.Name ?? "",
            IsManager = true,
            ActiveTab = ProjectTabs.Tasks
        };

        return new TaskManageViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Form = form,
            Categories = categories,
            Groups = groups
        };
    }
}