namespace TaskFlow.Models;

// Ekip içi rollerin adları (TeamMember.TeamRole alanına yazılır).
// Sistem rolleriyle (AppRoles) karıştırma: bunlar sadece O EKİP içinde geçerli.
public static class TeamRoles
{
    public const string Leader = "Lider"; // ekibi kuran / devralan: düzenler, üye ekler, devreder
    public const string Member = "Üye";   // ekibin projelerini görür, görevlerde çalışır
}