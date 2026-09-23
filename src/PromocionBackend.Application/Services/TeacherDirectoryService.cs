using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Teachers;

namespace PromocionBackend.Application.Services;

/// <summary>Búsqueda de docentes/autoridades por nombre o cédula, p.ej. para integrar comisiones.</summary>
public class TeacherDirectoryService(IHrApiClient hrApi)
{
    public async Task<IReadOnlyList<TeacherSummaryDto>> SearchAsync(string? query, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var teachers = await hrApi.SearchTeachersAsync(query, externalAccessToken, cancellationToken);

        return [.. teachers
            .Select(t => new TeacherSummaryDto(
                t.TeacherId,
                t.Identification,
                t.FullName,
                string.IsNullOrWhiteSpace(t.Dependency.Id) ? null : t.Dependency.Id,
                string.IsNullOrWhiteSpace(t.Dependency.Name) ? null : t.Dependency.Name))
            .OrderBy(t => t.FullName)];
    }
}
