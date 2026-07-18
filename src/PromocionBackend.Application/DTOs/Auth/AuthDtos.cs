using System.ComponentModel.DataAnnotations;

namespace PromocionBackend.Application.DTOs.Auth;

public class SessionRequest
{
    [Required(ErrorMessage = "El token externo es obligatorio.")]
    public string ExternalAccessToken { get; set; } = string.Empty;
}

public record SessionUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? TeacherId,
    string? Identification,
    string? CurrentPosition);

public record SessionResponse(string AccessToken, SessionUserDto User);
