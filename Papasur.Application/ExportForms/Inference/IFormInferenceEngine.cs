using Papasur.Application.Documentos.Inference;
using Papasur.Domain.ExportForms;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.ExportForms.Inference;

/// <summary>
/// Motor de inferencia de ámbito formulario: resuelve la ruta declarada en el requisito
/// (contrato §4) contra el envío completo — organización, cliente, cabecera y líneas.
///
/// Es el hermano de <see cref="IMotorInferencia"/>, que trabaja sobre un lote suelto. Los dos son
/// ASISTIVOS: proponen, y una persona confirma. Detrás de este puerto puede entrar un motor con
/// LLM sin tocar handlers ni controllers.
/// </summary>
public interface IFormInferenceEngine
{
    CampoInferido? Infer(string? path, ExportForm form, Cliente? customer);
}

/// <summary>
/// Datos del exportador que van en todos los documentos. Hoy son constantes; cuando exista
/// GET /organization salen de la base sin tocar el motor.
/// </summary>
public sealed record OrganizationProfile(
    string LegalName,
    string TaxId,
    string CountryName,
    string Province)
{
    public static readonly OrganizationProfile Default =
        new("Papasud S.A.", "30-XXXXXXXX-X", "Argentina", "Buenos Aires");
}
