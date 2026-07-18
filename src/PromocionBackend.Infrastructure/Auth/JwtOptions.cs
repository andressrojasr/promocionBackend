namespace PromocionBackend.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PromocionBackend";
    public string Audience { get; set; } = "PromocionBackend";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
