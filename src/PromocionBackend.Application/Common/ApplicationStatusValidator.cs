using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Common;

/// <summary>
/// Validador centralizado para estados de postulaciones.
/// Reemplaza validaciones dispersas en múltiples servicios.
/// </summary>
public static class ApplicationStatusValidator
{
    /// <summary>Valida si el string representa un estado de postulación válido.</summary>
    public static bool IsValidStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true; // null es válido (sin filtro)

        return ApplicationStatusExtensions.TryFromStringValue(status, out _);
    }

    /// <summary>Retorna mensaje de error para estado inválido.</summary>
    public static string GetValidationErrorMessage(string? status) =>
        $"Estado de postulación inválido: '{status}'. Valores válidos: {string.Join(", ", GetAllValidStatuses())}";

    /// <summary>Obtiene todos los valores de estado válidos.</summary>
    private static IEnumerable<string> GetAllValidStatuses() =>
        Enum.GetValues<ApplicationStatus>()
            .Select(s => s.ToStringValue());

    /// <summary>Valida una transición de estado.</summary>
    public static bool IsValidTransition(ApplicationStatus from, ApplicationStatus to, string reviewerRole)
    {
        var stage = ApplicationStateMachine.StageForRole(reviewerRole);
        if (stage == null) return false;

        var nextStatus = ApplicationStateMachine.GetNextStatus(from, stage, to.IsFinal());
        return nextStatus == to;
    }

    /// <summary>Obtiene un mensaje descriptivo para transición inválida.</summary>
    public static string GetTransitionErrorMessage(ApplicationStatus from, string reviewerRole) =>
        $"El revisor con rol '{reviewerRole}' no puede revisar una postulación en estado '{from.ToStringValue()}'";
}
