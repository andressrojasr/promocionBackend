namespace PromocionBackend.Domain.Services;

/// <summary>
/// Escalafón docente. Define las transiciones válidas de promoción.
/// Por reglamento no existe transición de AGREGADO_3 a PRINCIPAL_1,
/// y PRINCIPAL_3 es el tope del escalafón.
/// </summary>
public static class PositionLadder
{
    public const string Auxiliar1 = "AUXILIAR_1";
    public const string Auxiliar2 = "AUXILIAR_2";
    public const string Agregado1 = "AGREGADO_1";
    public const string Agregado2 = "AGREGADO_2";
    public const string Agregado3 = "AGREGADO_3";
    public const string Principal1 = "PRINCIPAL_1";
    public const string Principal2 = "PRINCIPAL_2";
    public const string Principal3 = "PRINCIPAL_3";

    private static readonly Dictionary<string, string> NextPosition = new(StringComparer.OrdinalIgnoreCase)
    {
        [Auxiliar1] = Auxiliar2,
        [Auxiliar2] = Agregado1,
        [Agregado1] = Agregado2,
        [Agregado2] = Agregado3,
        [Principal1] = Principal2,
        [Principal2] = Principal3
    };

    /// <summary>Posiciones desde las que se puede postular (tienen transición definida).</summary>
    public static readonly IReadOnlyList<string> TransitionFromPositions = [.. NextPosition.Keys];

    /// <summary>
    /// Devuelve la siguiente posición del escalafón, o null si no existe transición
    /// (AGREGADO_3 y PRINCIPAL_3).
    /// </summary>
    public static string? GetNextPosition(string currentPosition) =>
        NextPosition.TryGetValue(currentPosition, out var next) ? next : null;

    public static string Label(string position) => position switch
    {
        Auxiliar1 => "Titular Auxiliar 1",
        Auxiliar2 => "Titular Auxiliar 2",
        Agregado1 => "Titular Agregado 1",
        Agregado2 => "Titular Agregado 2",
        Agregado3 => "Titular Agregado 3",
        Principal1 => "Titular Principal 1",
        Principal2 => "Titular Principal 2",
        Principal3 => "Titular Principal 3",
        _ => position
    };
}
