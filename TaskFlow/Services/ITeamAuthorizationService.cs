using System.Security.Claims;

namespace TaskFlow.Services;

// "Bu kişi bu ekipte ne yapabilir?" sorularının TEK adresi.
// Hem TeamsController (Onur) hem ProjectsController (Zeynep) bunu kullanır.
// Sistem Admin'i her ekipte hem üye hem lider sayılır.
public interface ITeamAuthorizationService
{
    // Kişi bu ekibin üyesi mi? (ekibi ve projelerini GÖREBİLİR mi?)
    Task<bool> IsMemberAsync(ClaimsPrincipal user, int teamId);

    // Kişi bu ekibin lideri mi? (ekibi/projeleri YÖNETEBİLİR mi?)
    Task<bool> IsLeaderAsync(ClaimsPrincipal user, int teamId);

    // Kişinin üyesi olduğu ekiplerin Id'leri.
    // Zeynep: proje listesini ve "Ekip seç" açılır listesini filtrelemek için.
    // Admin için TÜM ekiplerin Id'leri döner.
    Task<List<int>> GetMemberTeamIdsAsync(ClaimsPrincipal user);
}