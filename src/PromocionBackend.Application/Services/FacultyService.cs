using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Faculties;

namespace PromocionBackend.Application.Services;

/// <summary>Catálogo de facultades (dependencias) de la Universidad, usado para filtrar postulaciones.</summary>
public class FacultyService(IHrApiClient hrApi)
{
    public async Task<IReadOnlyList<FacultyDto>> ListAsync(string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var faculties = await hrApi.GetFacultiesAsync(externalAccessToken, cancellationToken);
        return [.. faculties
            .Select(f => new FacultyDto(f.Id, f.Name))
            .OrderBy(f => f.Name)];
    }
}
