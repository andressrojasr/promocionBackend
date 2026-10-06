using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Infrastructure.Configuration;

namespace PromocionBackend.Infrastructure.ExternalServices;

/// <summary>
/// Cliente HTTP tipado hacia los servicios de RRHH de la UTA.
/// Soporta dos modos: simulado (backend simulado local) y real (servicios externos).
/// </summary>
public class HrApiClient(HttpClient httpClient, IOptions<DataSourceSettings> dataSourceOptions) : IHrApiClient
{
    private readonly DataSourceSettings _dataSourceSettings = dataSourceOptions.Value;

    private sealed class HrEnvelope<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public T? Data { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public async Task<string> GetIdentificationByEmailAsync(string email, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var identification = await GetAsync<string>(
                $"api/v1/rh/vw/EmployeeDetails/email/{Uri.EscapeDataString(email)}",
                externalAccessToken,
                notFoundMessage: "El correo no está registrado como empleado de la Universidad.",
                cancellationToken);

            return identification;
        }
        catch (Exception)
        {
            // Fallback: usar cédula estática si falla
            return _dataSourceSettings.StaticCedula;
        }
    }

    public async Task<HrTeacherDetails> GetTeacherDetailsAsync(string identification, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        // Para servicios reales de UTA, la URL incluye "WsUtaSystem"
        // Para BackendSimulado, la URL es directa
        var path = _dataSourceSettings.UseRealServices
            ? $"WsUtaSystem/api/v1/rh/academic-promotion/teachers/{Uri.EscapeDataString(identification)}"
            : $"api/v1/rh/academic-promotion/teachers/{Uri.EscapeDataString(identification)}";

        var details = await GetAsync<HrTeacherDetails>(
            path,
            externalAccessToken,
            notFoundMessage: "No se encontró la información docente asociada a su identificación.",
            cancellationToken);

        return details;
    }

    /// <summary>Tipo de departamento "FACULTAD" en el servicio de RRHH de la UTA.</summary>
    private const int FacultyDepartmentTypeId = 128;

    public async Task<IReadOnlyList<HrDependency>> GetFacultiesAsync(string externalAccessToken, CancellationToken cancellationToken = default)
    {
        // El servicio real y el simulado exponen el mismo contrato (arreglo de departamentos);
        // solo cambia el prefijo "WsUtaSystem" del servicio real.
        var path = _dataSourceSettings.UseRealServices
            ? $"WsUtaSystem/api/v1/rh/vw-departments/by-type/{FacultyDepartmentTypeId}"
            : $"api/v1/rh/vw-departments/by-type/{FacultyDepartmentTypeId}";

        var departments = await GetAsync<List<HrDepartment>>(
            path,
            externalAccessToken,
            notFoundMessage: "No se encontró el catálogo de facultades.",
            cancellationToken);

        return [.. departments
            .Where(d => d.IsActive && d.DepartmentTypeID == FacultyDepartmentTypeId)
            .Select(d => new HrDependency { Id = d.DepartmentID.ToString(), Name = d.DepartmentName })];
    }

    public async Task<IReadOnlyList<HrTeacherSummary>> SearchTeachersAsync(string? query, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var basePath = _dataSourceSettings.UseRealServices
            ? "WsUtaSystem/api/v1/rh/academic-promotion/teachers"
            : "api/v1/rh/academic-promotion/teachers";

        var path = string.IsNullOrWhiteSpace(query)
            ? basePath
            : $"{basePath}?query={Uri.EscapeDataString(query)}";

        return await GetAsync<List<HrTeacherSummary>>(
            path,
            externalAccessToken,
            notFoundMessage: "No se encontraron docentes.",
            cancellationToken);
    }

    private async Task<T> GetAsync<T>(string path, string externalAccessToken, string notFoundMessage, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", externalAccessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw AppException.UpstreamUnavailable(
                "No fue posible conectar con los servicios de la Universidad. Intente nuevamente más tarde.");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw AppException.Unauthorized("El token externo no es válido o ha expirado. Inicie sesión nuevamente.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw AppException.NotFound(notFoundMessage);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw AppException.UpstreamUnavailable(
                    $"Los servicios de la Universidad respondieron con un error ({(int)response.StatusCode}).");
            }

            // Lee el contenido como string para intentar ambos formatos
            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);

            // Intenta deserializar como envelope primero (BackendSimulado)
            try
            {
                var envelope = JsonSerializer.Deserialize<HrEnvelope<T>>(jsonString, AppJson.Options);
                if (envelope?.Success == true && envelope.Data is not null)
                {
                    return envelope.Data;
                }
            }
            catch
            {
                // Si no es un envelope válido, intentará el siguiente paso
            }

            // Si no hay envelope válido, intenta deserializar directamente como T (WsUtaSystem)
            try
            {
                var directData = JsonSerializer.Deserialize<T>(jsonString, AppJson.Options);
                if (directData is not null)
                {
                    return directData;
                }
            }
            catch
            {
                // Si tampoco funciona como objeto directo
            }

            throw AppException.NotFound(notFoundMessage);
        }
    }
}
