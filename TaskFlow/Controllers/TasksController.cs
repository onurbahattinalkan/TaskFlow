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

        var model = await BuildManageModelAsync(project, new TaskCreateViewModel { ProjectId = project.Id });
        return View(model);
    }

    // POST: /Tasks/Create  →  Görevi oluştur ve seçilen kişilere ata
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] TaskCreateViewModel form)
    {
        var project = await FindProjectAsync(form.ProjectId);
        if (project == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // Kategori + kişi + deadline kontrolleri (hata varsa ModelState'e eklenir)
        var category = await ValidateTaskFormAsync(project, form);

        if (!ModelState.IsValid)
            return View("Index", await BuildManageModelAsync(project, form));

        var nowUtc = DateTime.UtcNow;
        var selected = form.Assignees.Where(a => a.Selected).ToList();

        var task = new TaskItem
        {
            Title = form.Title.Trim(),
            Description = form.Description,
            ProjectId = project.Id,
            Category = category!,   // yeni kategoriyse EF onu da ekler
            CreatedAt = nowUtc
        };

        // Seçilen her kişi için bir atama
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
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{task.Title}\" görevi {selected.Count} kişiye atandı.";
        return RedirectToAction(nameof(Index), new { projectId = project.Id });
    }

    // GET: /Tasks/Edit/7  →  Pop-up'ın İÇERİĞİ (sadece form, sayfanın tamamı değil)
    public async Task<IActionResult> Edit(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, task.Project!.TeamId))
            return Forbid();

        // Formu görevin mevcut bilgileriyle doldur
        var form = new TaskCreateViewModel
        {
            TaskId = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            CategoryId = task.CategoryId,
            // Mevcut atananlar işaretli gelir, deadline'ları Türkiye saatine çevrilir
            Assignees = task.Assignments.Select(a => new AssigneeInput
            {
                UserId = a.UserId,
                Selected = true,
                DueDate = TimeHelper.ToTurkey(a.DueDate)
            }).ToList()
        };

        return PartialView("_EditTaskForm", await BuildFormModelAsync(task.Project, form, isEdit: true));
    }

    // POST: /Tasks/Edit  →  JavaScript (fetch) ile gönderilir.
    // Başarılı: { ok: true } (JSON) → sayfa yenilenir
    // Hatalı:   400 + hatalı form (HTML) → pop-up içinde gösterilir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "Form")] TaskCreateViewModel form)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == form.TaskId);

        if (task == null)
            return NotFound();

        // Proje formdan değil görevin kendisinden alınır (form elle değiştirilemesin)
        var project = task.Project!;
        form.ProjectId = project.Id;

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        // Mevcut deadline'lar: değiştirilmeyenler "geçmiş zaman" kontrolüne takılmasın
        var existingDue = task.Assignments.ToDictionary(a => a.UserId, a => a.DueDate);

        var category = await ValidateTaskFormAsync(project, form, existingDue);

        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;   // JavaScript bunu "hata var" diye anlar
            return PartialView("_EditTaskForm", await BuildFormModelAsync(project, form, isEdit: true));
        }

        var nowUtc = DateTime.UtcNow;

        // ---------- Ortak bilgiler ----------
        task.Title = form.Title.Trim();
        task.Description = form.Description;
        task.Category = category!;

        // ---------- Atamalar ----------
        var selected = form.Assignees.Where(a => a.Selected).ToList();
        var selectedIds = selected.Select(a => a.UserId).ToHashSet();

        // 1) İşareti kaldırılan kişilerin atamalarını sil
        foreach (var old in task.Assignments.Where(a => !selectedIds.Contains(a.UserId)).ToList())
        {
            _context.TaskAssignments.Remove(old);
        }

        // 2) Seçili kişiler: yeni ise ekle, varsa deadline'ını güncelle
        foreach (var a in selected)
        {
            var dueUtc = TimeHelper.ToUtc(a.DueDate!.Value);
            var existing = task.Assignments.FirstOrDefault(x => x.UserId == a.UserId);

            if (existing == null)
            {
                task.Assignments.Add(new TaskAssignment
                {
                    UserId = a.UserId,
                    DueDate = dueUtc,
                    Status = TaskItemStatus.Todo,
                    AssignedAt = nowUtc
                });
            }
            else if (Math.Abs((existing.DueDate - dueUtc).TotalMinutes) >= 1)
            {
                // Sadece gerçekten değiştiyse (saniye farklarını yok say)
                existing.DueDate = dueUtc;
            }
            // Durum (Status) ve atanma tarihi (AssignedAt) korunur
        }

        await _context.SaveChangesAsync();

        // Sayfa yenilenince görünecek mesaj
        TempData["Success"] = $"\"{task.Title}\" görevi güncellendi.";
        return Json(new { ok = true });
    }

    
    // POST: /Tasks/DeleteAssignment/12  →  Görevi SADECE bu kişiden kaldır
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        // Atamayı görevi, projesi ve kişisiyle birlikte bul (mesaj ve yetki için)
        var assignment = await _context.TaskAssignments
            .Include(a => a.TaskItem).ThenInclude(t => t!.Project)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
            return NotFound();

        var task = assignment.TaskItem!;

        if (!await _teamAuth.IsLeaderAsync(User, task.Project!.TeamId))
            return Forbid();

        _context.TaskAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        // Görevde başka atanan kalmadıysa görevi de sil
        var removedTasks = await RemoveTasksWithoutAssignmentsAsync(new[] { task.Id });

        TempData["Success"] = $"\"{task.Title}\" görevi {assignment.User?.UserName} kişisinden kaldırıldı."
            + (removedTasks > 0 ? " Başka atanan kalmadığı için görev tamamen silindi." : "");

        return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
    }

    // POST: /Tasks/DeleteAssignments  →  İşaretlenen atamaların hepsini sil (Tümünü seç + Seçilenleri sil)
    // ids: işaretli satırların atama Id'leri (checkbox'ların name="ids" değerleri)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignments(int projectId, List<int> ids)
    {
        var project = await FindProjectAsync(projectId);
        if (project == null)
            return NotFound();

        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();

        if (ids == null || ids.Count == 0)
        {
            TempData["Error"] = "Silinecek atama seçilmedi.";
            return RedirectToAction(nameof(Index), new { projectId });
        }

        // Sadece BU projeye ait atamalar (başka projenin Id'si gönderilirse yok sayılır)
        var assignments = await _context.TaskAssignments
            .Where(a => ids.Contains(a.Id) && a.TaskItem!.ProjectId == projectId)
            .ToListAsync();

        // Etkilenen görevler (sonra boş kalanları sileceğiz)
        var taskIds = assignments.Select(a => a.TaskItemId).Distinct().ToList();

        _context.TaskAssignments.RemoveRange(assignments);
        await _context.SaveChangesAsync();

        var removedTasks = await RemoveTasksWithoutAssignmentsAsync(taskIds);

        TempData["Success"] = $"{assignments.Count} atama silindi."
            + (removedTasks > 0 ? $" Kimseye atanmış hali kalmayan {removedTasks} görev de tamamen silindi." : "");

        return RedirectToAction(nameof(Index), new { projectId });
    }

    // Verilen görevlerden HİÇ ataması kalmayanları siler, kaç tane sildiğini döner.
    // (Kimseye atanmamış görev listede görünmez, veritabanında boşuna kalmasın.)
    private async Task<int> RemoveTasksWithoutAssignmentsAsync(IEnumerable<int> taskIds)
    {
        var ids = taskIds.ToList();

        var emptyTasks = await _context.Tasks
            .Where(t => ids.Contains(t.Id) && !t.Assignments.Any())
            .ToListAsync();

        if (emptyTasks.Count == 0)
            return 0;

        _context.Tasks.RemoveRange(emptyTasks);
        await _context.SaveChangesAsync();
        return emptyTasks.Count;
    }


    // ---------- Yardımcı metotlar ----------

    // Projeyi ekibiyle birlikte bul
    private Task<Project?> FindProjectAsync(int projectId) =>
        _context.Projects.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == projectId);

    // Create ve Edit'in ORTAK kontrolleri. Hataları ModelState'e ekler, kategoriyi döner.
    // existingDue: düzenlemede kişilerin mevcut deadline'ları (UTC); oluşturmada null
    private async Task<Category?> ValidateTaskFormAsync(
        Project project, TaskCreateViewModel form, Dictionary<string, DateTime>? existingDue = null)
    {
        // ---------- Kategori ----------
        Category? category = null;
        var newName = form.NewCategoryName?.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            // Aynı isim zaten varsa onu kullan (büyük/küçük harf duyarsız)
            category = await _context.Categories.FirstOrDefaultAsync(c =>
                c.ProjectId == project.Id && c.Name.ToLower() == newName.ToLower());

            if (category == null)
            {
                // Renk paletten değilse varsayılan
                var color = CategoryColors.All.ContainsKey(form.NewCategoryColor)
                    ? form.NewCategoryColor
                    : CategoryColors.Default;

                category = new Category { Name = newName, Color = color, ProjectId = project.Id };
            }
        }
        else if (form.CategoryId.HasValue)
        {
            // Seçilen kategori bu projeye mi ait?
            category = await _context.Categories.FirstOrDefaultAsync(c =>
                c.Id == form.CategoryId && c.ProjectId == project.Id);
        }

        if (category == null)
            ModelState.AddModelError("Form.CategoryId", "Bir kategori seçin ya da yeni kategori adı yazın.");

        // ---------- Kişiler ve deadline'lar ----------
        var memberIds = await _context.TeamMembers
            .Where(m => m.TeamId == project.TeamId)
            .Select(m => m.UserId)
            .ToListAsync();

        if (!form.Assignees.Any(a => a.Selected))
            ModelState.AddModelError("Form.Assignees", "En az bir kişi seçmelisiniz.");

        var nowUtc = DateTime.UtcNow;

        for (int i = 0; i < form.Assignees.Count; i++)
        {
            var a = form.Assignees[i];
            if (!a.Selected)
                continue;

            if (!memberIds.Contains(a.UserId))
            {
                ModelState.AddModelError("Form.Assignees", "Seçilen kişilerden biri bu projenin ekibinde değil.");
                continue;
            }

            if (a.DueDate == null)
            {
                ModelState.AddModelError($"Form.Assignees[{i}].DueDate", "Deadline seçin.");
                continue;
            }

            var dueUtc = TimeHelper.ToUtc(a.DueDate.Value);

            // Düzenlemede bu kişinin deadline'ı DEĞİŞMEDİYSE geçmiş olması sorun değil
            var unchanged = existingDue != null
                && existingDue.TryGetValue(a.UserId, out var oldDue)
                && Math.Abs((oldDue - dueUtc).TotalMinutes) < 1;

            if (!unchanged && dueUtc <= nowUtc)
                ModelState.AddModelError($"Form.Assignees[{i}].DueDate", "Deadline geçmiş bir zaman olamaz.");
        }

        return category;
    }

    // Ortak form partial'ının modeli: kişi listesi (ekip üyeleri) + kategoriler
    private async Task<TaskFormModel> BuildFormModelAsync(Project project, TaskCreateViewModel form, bool isEdit)
    {
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == project.TeamId)
            .OrderBy(m => m.User.UserName)
            .Select(m => new { m.UserId, m.User.UserName, m.User.FullName })
            .ToListAsync();

        // Kişi listesini ekip üyelerinden kur; formdan gelen seçim ve deadline'lar korunsun
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

        var categories = await _context.Categories
            .Where(c => c.ProjectId == project.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return new TaskFormModel { Form = form, Categories = categories, IsEdit = isEdit };
    }

    // Yönetici sayfasının modeli (GET ve hatalı Create POST kullanır)
    private async Task<TaskManageViewModel> BuildManageModelAsync(Project project, TaskCreateViewModel form)
    {
        var formModel = await BuildFormModelAsync(project, form, isEdit: false);

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

        // Kişiye göre grupla; grup içinde deadline artan, eşitse önce oluşturulan üstte
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
            Form = formModel.Form,
            Categories = formModel.Categories,
            Groups = groups
        };
    }
}