using Papasur.Application.Abstractions;
using Papasur.Application.Lots.Ports;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms.Commands;

/// <summary>
/// Convierte las líneas que manda el cliente en líneas de dominio, congelando la trazabilidad
/// del lote en ese momento. Si el lote cambia después, el documento emitido no miente (plan §1.3).
/// </summary>
public sealed class FormItemBuilder(ILotProjectionRepository lots)
{
    public async Task<Result<List<ExportFormItem>>> BuildAsync(
        IReadOnlyList<FormItemInput> inputs,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
        {
            return Result.Success(new List<ExportFormItem>());
        }

        var found = await lots.GetByIdsAsync([.. inputs.Select(i => i.LotId).Distinct()], cancellationToken);
        var byId = found.ToDictionary(l => l.Id);
        var now = DateTime.UtcNow;
        var items = new List<ExportFormItem>(inputs.Count);
        var position = 0;

        foreach (var input in inputs)
        {
            if (!byId.TryGetValue(input.LotId, out var lot))
            {
                return Result.Failure<List<ExportFormItem>>(
                    new Error("Form.LotNotFound", "Una de las líneas apunta a un lote que no existe."));
            }

            if (input.QuantityKg <= 0)
            {
                return Result.Failure<List<ExportFormItem>>(
                    new Error("Form.QuantityInvalid", $"La cantidad del lote {lot.Code} tiene que ser mayor que cero."));
            }

            if (!PackagingTypes.Exists(input.PackagingType))
            {
                return Result.Failure<List<ExportFormItem>>(
                    new Error("Form.PackagingInvalid", "El tipo de envase no es válido."));
            }

            if (input.UnitPrice < 0)
            {
                return Result.Failure<List<ExportFormItem>>(
                    new Error("Form.UnitPriceInvalid", "El precio unitario no puede ser negativo."));
            }

            items.Add(new ExportFormItem
            {
                Id = Guid.NewGuid(),
                LotId = lot.Id,
                QuantityKg = input.QuantityKg,
                PackagingType = input.PackagingType,
                PackagesCount = FormCalculations.PackagesFor(input.PackagingType, input.QuantityKg),
                UnitPrice = input.UnitPrice,
                LineTotal = FormCalculations.LineTotal(input.QuantityKg, input.UnitPrice),
                Position = position++,
                Traceability = new TraceabilitySnapshot
                {
                    LotCode = lot.Code,
                    Species = lot.Species,
                    Variety = lot.Variety,
                    Category = lot.Category,
                    CropYear = lot.CropYear,
                    LocationCode = lot.LocationCode,
                    GerminationRate = lot.GerminationRate,
                    Purity = lot.Purity,
                    InaseRegistration = lot.InaseRegistration,
                    CapturedAt = now,
                },
            });
        }

        return Result.Success(items);
    }
}
