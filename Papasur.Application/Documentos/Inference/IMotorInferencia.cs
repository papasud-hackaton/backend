using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Documentos.Inference;

/// <summary>
/// Motor de inferencia: dado el requisito de un campo (su regla de mapeo) y la trazabilidad de un
/// lote/movimiento, propone el valor que ya se conoce. La implementación por defecto es determinística
/// (reglas de mapeo). Una futura implementación con LLM viviría en Infrastructure detrás de este puerto.
/// Siempre es ASISTIVA: lo que devuelve es una sugerencia que un humano confirma.
/// </summary>
public interface IMotorInferencia
{
    /// <summary>
    /// Resuelve el valor sugerido para una regla de mapeo. Devuelve null si no hay regla o no se pudo
    /// resolver: en ese caso el campo queda pendiente de completar a mano o por dictado.
    /// </summary>
    CampoInferido? Inferir(string? reglaMapeo, Lote lote, Movimiento? movimiento);
}

/// <summary>Sugerencia de valor para un campo, con la traza de dónde salió.</summary>
public sealed record CampoInferido(string Valor, string InferidoDesde);
