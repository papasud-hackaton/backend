using System.Globalization;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Documentos.Inference;

/// <summary>
/// Implementación determinística del motor de inferencia: cruza la <c>ReglaMapeo</c> de cada campo
/// (ej. "movimiento.dtv", "lote.variedad") con la trazabilidad del lote y el movimiento elegido.
/// Es la base estable para la demo; los campos sin regla o sin dato quedan para el humano.
/// </summary>
public sealed class MotorInferenciaReglas : IMotorInferencia
{
    public CampoInferido? Inferir(string? reglaMapeo, Lote lote, Movimiento? movimiento)
    {
        if (string.IsNullOrWhiteSpace(reglaMapeo))
        {
            return null;
        }

        var regla = reglaMapeo.Trim().ToLowerInvariant();
        var valor = Resolver(regla, lote, movimiento);

        return string.IsNullOrWhiteSpace(valor) ? null : new CampoInferido(valor, regla);
    }

    private static string? Resolver(string regla, Lote lote, Movimiento? m) => regla switch
    {
        "lote.codigo" => lote.Codigo,
        "lote.variedad" => lote.Variedad?.Nombre,
        "lote.campo" => lote.Campo?.Nombre,
        "lote.establecimiento" => lote.Campo?.Establecimiento,
        "lote.categoria" => lote.Categoria,
        "lote.superficie_ha" => Num(lote.SuperficieHa),

        "movimiento.numero_remito" => m?.NumeroRemito,
        "movimiento.fecha" => m?.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        "movimiento.kilogramos" => Num(m?.Kilogramos),
        "movimiento.bolsas" => m?.Bolsas?.ToString(CultureInfo.InvariantCulture),
        "movimiento.kg_promedio" => Num(m?.KgPromedio),
        "movimiento.presentacion" => m?.Presentacion,
        "movimiento.categoria" => m?.Categoria,
        "movimiento.calibre" => m?.Calibre,
        "movimiento.transportista" => m?.Transportista?.Nombre,
        "movimiento.cliente" => m?.Cliente?.Nombre,
        "movimiento.pais" => m?.Cliente?.Pais,
        "movimiento.comisionista" => m?.Comisionista,
        "movimiento.destino" => m?.Destino,
        "movimiento.dtv" => m?.Dtv,

        _ => null,
    };

    private static string? Num(decimal? value)
        => value?.ToString("0.###", CultureInfo.InvariantCulture);
}
