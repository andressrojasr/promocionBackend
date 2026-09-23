using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PromocionBackend.Infrastructure.Persistence;

/// <summary>
/// SQL Server (datetime2) no guarda el "Kind" del DateTime: todo lo que EF Core lee vuelve
/// con Kind=Unspecified, aunque la app siempre escribe DateTime.UtcNow. Al serializar a JSON,
/// un DateTime Unspecified no lleva sufijo "Z", así que el navegador lo interpreta como hora
/// LOCAL en vez de UTC — mostrando la hora con un desfase (el bug que se reportó en comisiones
/// y sesiones de revisión). Este conversor global re-marca cada valor leído como Utc (sin
/// cambiar el número), para que toda la app sea consistente sin tener que parchear cada DTO.
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc));

public class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v,
    v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
