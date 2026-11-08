using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Services;

public class TeamAuthorizationService : ITeamAuthorizationService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamAuthorizationService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> IsMemberAsync(ClaimsPrincipal user, int teamId)
    {
        // Admin her ekibe erişebilir (çerezdeki rol bilgisi, veritabanına gitmeden)
        if (user.IsInRole(AppRoles.Admin))
            return true;

        var userId = _userManager.GetUserId(user);
        if (userId == null)
            return false; // giriş yapılmamış

        // AnyAsync: "en az bir satır var mı?" → SQL'de EXISTS; satırları çekmez, hızlıdır
        return await _context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
    }

    public async Task<bool> IsLeaderAsync(ClaimsPrincipal user, int teamId)
    {
        if (user.IsInRole(AppRoles.Admin))
            return true;

        var userId = _userManager.GetUserId(user);
        if (userId == null)
            return false;

        // Üyelik + rolün Lider olması
        return await _context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId
                        && m.UserId == userId
                        && m.TeamRole == TeamRoles.Leader);
    }

    public async Task<List<int>> GetMemberTeamIdsAsync(ClaimsPrincipal user)
    {
        // Admin: tüm ekipler
        if (user.IsInRole(AppRoles.Admin))
            return await _context.Teams.Select(t => t.Id).ToListAsync();

        var userId = _userManager.GetUserId(user);
        if (userId == null)
            return new List<int>();

        // Kullanıcının üyelik satırlarındaki ekip Id'leri
        return await _context.TeamMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId)
            .ToListAsync();
    }
    public async Task<List<int>> GetLeaderTeamIdsAsync(ClaimsPrincipal user)
    {
        // Admin: tüm ekiplerde lider sayılır
        if (user.IsInRole(AppRoles.Admin))
            return await _context.Teams.Select(t => t.Id).ToListAsync();

        var userId = _userManager.GetUserId(user);
        if (userId == null)
            return new List<int>();

        // Kullanıcının "Lider" rolüyle bulunduğu ekip satırları
        return await _context.TeamMembers
            .Where(m => m.UserId == userId && m.TeamRole == TeamRoles.Leader)
            .Select(m => m.TeamId)
            .ToListAsync();
    }
}