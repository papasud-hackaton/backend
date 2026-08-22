using Papasur.Domain.Documentos;

namespace Papasur.Application.Documentos.Ports;

/// <summary>
/// Puerto de persistencia de documentos de exportación generados. Implementado en Infrastructure.
/// </summary>
public interface IDocumentoRepository
{
    Task AddAsync(DocumentoExportacion documento, CancellationToken cancellationToken);

    /// <summary>Trae el documento con sus valores (+ campo de plantilla), plantilla, lote, movimiento y estado.</summary>
    Task<DocumentoExportacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(DocumentoExportacion documento, CancellationToken cancellationToken);

    /// <summary>
    /// Reemplaza los documentos de un formulario: regenerar rehace el juego completo, no acumula
    /// versiones sueltas que después nadie sabe cuál es la buena.
    /// </summary>
    Task ReplaceForFormAsync(
        Guid exportFormId,
        IReadOnlyList<DocumentoExportacion> documentos,
        CancellationToken cancellationToken);
}
