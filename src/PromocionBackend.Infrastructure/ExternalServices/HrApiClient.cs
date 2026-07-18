using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;

namespace PromocionBackend.Infrastructure.ExternalServices;

/// <summary>
/// Cliente HTTP tipado hacia los servicios de RRHH de la UTA (backend simulado).
/// Reenvía el token externo del usuario como Bearer, de modo que la validación
/// de la firma RS256 queda delegada al sistema externo.
/// </summary>
public class HrApiClient(HttpClient httpClient) : IHrApiClient
{
    private sealed class HrEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }

    public async Task<string> GetIdentificationByEmailAsync(string email, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var identification = await GetAsync<string>(
            $"api/v1/rh/vw/EmployeeDetails/email/{Uri.EscapeDataString(email)}",
            externalAccessToken,
            notFoundMessage: "El correo no está registrado como empleado de la Universidad.",
            cancellationToken);

        return identification;
    }

    public async Task<HrTeacherDetails> GetTeacherDetailsAsync(string identification, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var details = await GetAsync<HrTeacherDetails>(
            $"api/v1/rh/academic-promotion/teachers/{Uri.EscapeDataString(identification)}",
            externalAccessToken,
            notFoundMessage: "No se encontró la información docente asociada a su identificación.",
            cancellationToken);

        return details;
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

            var envelope = await response.Content.ReadFromJsonAsync<HrEnvelope<T>>(AppJson.Options, cancellationToken);

            if (envelope is null || !envelope.Success || envelope.Data is null)
            {
                throw AppException.NotFound(envelope?.Message ?? notFoundMessage);
            }

            return envelope.Data;
        }
    }
}
