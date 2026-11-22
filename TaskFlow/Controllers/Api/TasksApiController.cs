using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Dtos;
using TaskFlow.Models;
using TaskFlow.Services;

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
}