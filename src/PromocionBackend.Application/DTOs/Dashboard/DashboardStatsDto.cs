namespace PromocionBackend.Application.DTOs.Dashboard;

/// <summary>
/// Contadores del dashboard según el rol del usuario autenticado.
/// Las claves dependen del rol (p. ej. "pendingReview", "openProcesses").
/// </summary>
public record DashboardStatsDto(string Role, IReadOnlyDictionary<string, int> Counters);
