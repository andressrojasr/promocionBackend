using PromocionBackend.Application.DTOs.Actas;

namespace PromocionBackend.Application.Abstractions;

/// <summary>Genera el PDF del acta de promoción a partir de datos ya resueltos.</summary>
public interface IActaPdfBuilder
{
    byte[] Build(ActaData data);
}
