using PromocionBackend.Application.Abstractions.External;

namespace PromocionBackend.Application.DTOs.Teachers;

/// <summary>
/// Hoja de vida del docente tal como se capturó del sistema de RRHH en el último
/// inicio de sesión, junto con su posición actual y la siguiente del escalafón.
/// </summary>
public record TeacherProfileDto(
    DateTime CapturedAt,
    string CurrentPosition,
    string CurrentPositionLabel,
    string? NextPosition,
    string? NextPositionLabel,
    HrTeacherDetails Profile);
