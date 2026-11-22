using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Dtos;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Helpers;   // CategoryColors için

namespace TaskFlow.Controllers.Api;

// Görevler için REST API.
// [ApiController]: JSON → DTO çevirisi, otomatik 400 (doğrulama), standart hata gövdesi
// [Route]: tüm uç noktalar /api/tasks altında
// [Authorize]: giriş zorunlu (girişsiz istek 401 alır, Login'e yönlenmez)
[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITeamAuthorizationService _teamAuth;

    public TasksApiController(AppDbContext context, ITeamAuthorizationService teamAuth)
    {
        _context = context;
        _teamAuth = teamAuth;
    }

    // GET /api/tasks?projectId=1  →  Projenin görevleri
    // Lider/Admin: tüm görevler + tüm atamalar
    // Normal üye: sadece kendisine atanan görevler + sadece kendi ataması
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetTasks([FromQuery] int projectId)
    {
        // Proje var mı? (sadece ekip Id'si lazım, tüm satırı çekmeye gerek yok)
        var teamId = await _context.Projects
            .Where(p => p.Id == projectId)
            .Select(p => (int?)p.TeamId)   // (int?): proje yoksa 0 değil null gelsin
            .FirstOrDefaultAsync();

        if (teamId == null)
            return NotFound();   // 404

        // Ekipte değilse göremez
        if (!await _teamAuth.IsMemberAsync(User, teamId.Value))
            return Forbid();     // 403

        var isManager = await _teamAuth.IsLeaderAsync(User, teamId.Value);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Tasks.Where(t => t.ProjectId == projectId);

        // Normal üye: sadece en az bir ataması kendisine ait olan görevler
        if (!isManager)
            query = query.Where(t => t.Assignments.Any(a => a.UserId == userId));

        var tasks = await ToDto(query.OrderBy(t => t.CreatedAt), isManager, userId)
            .ToListAsync();

        return tasks;   // 200 + JSON dizi
    }

    // GET /api/tasks/5  →  Tek görev
    // {id:int}: sadece sayı kabul edilir (/api/tasks/abc → 404)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> GetTask(int id)
    {
        // Görevin projesinin ekibi
        var teamId = await _context.Tasks
            .Where(t => t.Id == id)
            .Select(t => (int?)t.Project!.TeamId)
            .FirstOrDefaultAsync();

        if (teamId == null)
            return NotFound();

        if (!await _teamAuth.IsMemberAsync(User, teamId.Value))
            return Forbid();

        var isManager = await _teamAuth.IsLeaderAsync(User, teamId.Value);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Tasks.Where(t => t.Id == id);

        if (!isManager)
            query = query.Where(t => t.Assignments.Any(a => a.UserId == userId));

        var task = await ToDto(query, isManager, userId).FirstOrDefaultAsync();

        // Üye ama bu görev ona atanmamış → onun açısından böyle bir görev yok
        if (task == null)
            return NotFound();

        return task;   // 200 + JSON nesne
    }

    // POST /api/tasks  →  Yeni görev oluştur ve seçilen kişilere ata (sadece Lider/Admin)
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask(TaskCreateDto dto)
    {
        // Buraya gelindiyse DTO kuralları ([Required] vb.) zaten geçti; bozuksa otomatik 400 dönerdi

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
        if (project == null)
            return NotFound();   // 404

        // Görev oluşturmak yöneticiye özel
        if (!await _teamAuth.IsLeaderAsync(User, project.TeamId))
            return Forbid();     // 403

        // Veritabanı gerektiren kontroller (hatalar ModelState'e eklenir)
        var category = await ResolveCategoryAsync(project.Id, dto);
        await ValidateAssigneesAsync(project.TeamId, dto.Assignees);

        // Hata varsa otomatik 400 ile AYNI biçimde cevap ver
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var nowUtc = DateTime.UtcNow;

        var task = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            ProjectId = project.Id,
            Category = category!,   // yeni kategoriyse EF onu da ekler
            CreatedAt = nowUtc
        };

        // Gönderilen her kişi için bir atama
        foreach (var a in dto.Assignees)
        {
            task.Assignments.Add(new TaskAssignment
            {
                UserId = a.UserId,
                // DateTimeOffset → UTC (hangi saat diliminden gelirse gelsin doğru çevrilir)
                DueDate = a.DueDate!.Value.UtcDateTime,
                Status = TaskItemStatus.Todo,
                AssignedAt = nowUtc
            });
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Kaydedilen görevi kullanıcı adları ve kategoriyle birlikte DTO olarak geri oku
        var result = await ToDto(_context.Tasks.Where(t => t.Id == task.Id), isManager: true, userId: null)
            .FirstAsync();

        // 201 Created + Location: /api/tasks/{id} + gövdede yeni görev
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, result);
    }

    // ---------- Yardımcı metotlar ----------

    // TaskItem sorgusunu TaskDto'ya çevirir (EF bunu tek bir SQL sorgusuna dönüştürür).
    // isManager false ise atamalardan sadece giriş yapan kişininki gelir.
    private static IQueryable<TaskDto> ToDto(IQueryable<TaskItem> tasks, bool isManager, string? userId) =>
        tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ProjectId = t.ProjectId,
            CreatedAt = t.CreatedAt,
            Category = new CategoryDto
            {
                Id = t.Category!.Id,
                Name = t.Category.Name,
                Color = t.Category.Color
            },
            Assignments = t.Assignments
                .Where(a => isManager || a.UserId == userId)
                .OrderBy(a => a.DueDate)
                .Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User!.UserName ?? "",
                    FullName = a.User.FullName,
                    DueDate = a.DueDate,
                    Status = a.Status,
                    AssignedAt = a.AssignedAt
                })
                .ToList()
        });


    // Kategoriyi belirler: yeni ad yazıldıysa onu bulur/oluşturur, yoksa seçilen Id'yi doğrular.
    // Bulamazsa ModelState'e hata ekler ve null döner. (POST ve PUT ortak kullanır)
    private async Task<Category?> ResolveCategoryAsync(int projectId, TaskUpdateDto dto)
    {
        var newName = dto.NewCategoryName?.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            // Aynı isim projede zaten varsa onu kullan (büyük/küçük harf duyarsız)
            var existing = await _context.Categories.FirstOrDefaultAsync(c =>
                c.ProjectId == projectId && c.Name.ToLower() == newName.ToLower());

            if (existing != null)
                return existing;

            // Renk paletten değilse varsayılan renk
            var color = dto.NewCategoryColor?.Trim().ToLowerInvariant();
            if (color == null || !CategoryColors.All.ContainsKey(color))
                color = CategoryColors.Default;

            return new Category { Name = newName, Color = color, ProjectId = projectId };
        }

        if (dto.CategoryId.HasValue)
        {
            // Seçilen kategori bu projeye mi ait?
            var category = await _context.Categories.FirstOrDefaultAsync(c =>
                c.Id == dto.CategoryId && c.ProjectId == projectId);

            if (category != null)
                return category;

            ModelState.AddModelError(nameof(dto.CategoryId), "Kategori bu projede bulunamadı.");
            return null;
        }

        ModelState.AddModelError(nameof(dto.CategoryId), "CategoryId ya da NewCategoryName gönderilmelidir.");
        return null;
    }

    // Atanan kişileri kontrol eder; hataları ModelState'e ekler. (POST ve PUT ortak kullanır)
    // existingDue: düzenlemede kişilerin mevcut son tarihleri (UTC); oluşturmada null
    private async Task ValidateAssigneesAsync(
        int teamId, List<AssigneeDto> assignees, Dictionary<string, DateTime>? existingDue = null)
    {
        // Ekip üyelerinin Id'leri (HashSet: Contains hızlı çalışır)
        var memberIds = (await _context.TeamMembers
            .Where(m => m.TeamId == teamId)
            .Select(m => m.UserId)
            .ToListAsync()).ToHashSet();

        var seen = new HashSet<string>();   // aynı kişi iki kez gönderilmesin
        var nowUtc = DateTime.UtcNow;

        for (int i = 0; i < assignees.Count; i++)
        {
            var a = assignees[i];
            // Hata anahtarı otomatik 400'deki gibi: "Assignees[0].UserId"
            var key = $"{nameof(TaskUpdateDto.Assignees)}[{i}]";

            if (!seen.Add(a.UserId))
            {
                ModelState.AddModelError($"{key}.{nameof(AssigneeDto.UserId)}", "Aynı kişi birden fazla kez gönderildi.");
                continue;
            }

            if (!memberIds.Contains(a.UserId))
            {
                ModelState.AddModelError($"{key}.{nameof(AssigneeDto.UserId)}", "Bu kişi projenin ekibinde değil.");
                continue;
            }

            var dueUtc = a.DueDate!.Value.UtcDateTime;

            // Düzenlemede bu kişinin son tarihi DEĞİŞMEDİYSE geçmişte olması sorun değil
            var unchanged = existingDue != null
                && existingDue.TryGetValue(a.UserId, out var oldDue)
                && Math.Abs((oldDue - dueUtc).TotalMinutes) < 1;

            if (!unchanged && dueUtc <= nowUtc)
                ModelState.AddModelError($"{key}.{nameof(AssigneeDto.DueDate)}", "Son tarih geçmiş bir zaman olamaz.");
        }
    }

}