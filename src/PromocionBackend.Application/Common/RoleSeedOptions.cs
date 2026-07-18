namespace PromocionBackend.Application.Common;

/// <summary>
/// Semilla de roles por correo: se aplica únicamente cuando el usuario inicia
/// sesión por primera vez. Después, el rol registrado en la base de datos manda
/// y solo un administrador puede cambiarlo.
/// </summary>
public class RoleSeedOptions
{
    public const string SectionName = "RoleSeeds";

    public Dictionary<string, string> Emails { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
