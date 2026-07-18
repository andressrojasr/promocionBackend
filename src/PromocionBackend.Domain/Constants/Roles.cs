namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Roles de la aplicación. Se persisten como texto para facilitar su lectura en la base de datos.
/// </summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Cp = "cp";
    public const string Th = "th";
    public const string Ca = "ca";
    public const string Teacher = "teacher";

    public static readonly IReadOnlyList<string> All = [Admin, Cp, Th, Ca, Teacher];

    public static bool IsValid(string role) => All.Contains(role);
}
