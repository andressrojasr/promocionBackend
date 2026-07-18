using System.Text.Json;

namespace PromocionBackend.Application.Common;

/// <summary>
/// Opciones de serialización compartidas (camelCase, tolerante a mayúsculas),
/// usadas para los snapshots persistidos y la comunicación con el sistema de RRHH.
/// </summary>
public static class AppJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
