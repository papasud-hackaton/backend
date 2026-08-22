using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papasur.Domain.Documentos;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// Siembra los SEIS requisitos documentales de ámbito formulario (contrato §4) como DATO.
///
/// Salen de la hipótesis que el front venía usando; acá dejan de ser código para ser filas de
/// plantilla_documento + campo_plantilla. Cuando Papasud entregue las plantillas y la normativa
/// reales, se editan estas filas —o se cargan por API— y ni el front ni el backend cambian.
///
/// Cada campo declara su ReglaMapeo (la ruta de la que se autocompleta): es lo que el motor de
/// inferencia de formulario resuelve al generar la documentación.
/// </summary>
public sealed class RequisitosSeeder(AppDbContext db, ILogger<RequisitosSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await db.PlantillasDocumento
            .Where(p => p.Ambito == AmbitosPlantilla.Formulario)
            .Select(p => p.Codigo)
            .ToListAsync(cancellationToken);

        var pending = Templates().Where(p => !existing.Contains(p.Codigo)).ToList();

        if (pending.Count == 0)
        {
            return;
        }

        db.PlantillasDocumento.AddRange(pending);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Requisitos documentales sembrados: {Count} plantillas de formulario.", pending.Count);
    }

    private static IEnumerable<PlantillaDocumento> Templates()
    {
        yield return Template(
            CodigosDocumento.ProformaInvoice, "Factura proforma", TiposDocumento.Proforma, "Papasud S.A.",
            [
                Field("exporter_name", "Exportador", OrigenesCampo.Organization, true, "organization.legalName"),
                Field("exporter_tax_id", "CUIT del exportador", OrigenesCampo.Organization, true, "organization.taxId"),
                Field("buyer_name", "Comprador", OrigenesCampo.Customer, true, "customer.name"),
                Field("buyer_tax_id", "Identificación fiscal del comprador", OrigenesCampo.Customer, true, "customer.taxId"),
                Field("buyer_address", "Domicilio del comprador", OrigenesCampo.Customer, true, "customer.address"),
                Field("destination_country", "País de destino", OrigenesCampo.Form, true, "customer.countryName"),
                Field("incoterm", "Incoterm", OrigenesCampo.Form, true, "form.incoterm"),
                Field("currency", "Moneda", OrigenesCampo.Form, true, "form.currency"),
                Field("port_of_loading", "Puerto de embarque", OrigenesCampo.Form, true, "form.portOfLoading"),
                Field("port_of_discharge", "Puerto de descarga", OrigenesCampo.Form, true, "form.portOfDischarge"),
                Field("payment_terms", "Condición de pago", OrigenesCampo.Form, true, "form.paymentTerms"),
                Field("valid_until", "Validez de la oferta", OrigenesCampo.Form, true, "form.validUntil", TiposDato.Fecha),
                Field("total_amount", "Importe total", OrigenesCampo.Form, true, "form.totals.totalAmount", TiposDato.Numero),
            ]);

        yield return Template(
            CodigosDocumento.PackingList, "Packing list", TiposDocumento.PackingList, "Papasud S.A.",
            [
                Field("lot_code", "Lote", OrigenesCampo.Lot, true, "items[].traceability.lotCode"),
                Field("packaging_type", "Tipo de envase", OrigenesCampo.Form, true, "items[].packagingType"),
                Field("packages_count", "Cantidad de bultos", OrigenesCampo.Form, true, "items[].packagesCount", TiposDato.Numero),
                Field("net_weight", "Peso neto", OrigenesCampo.Form, true, "items[].quantityKg", TiposDato.Numero),
                Field("total_packages", "Total de bultos", OrigenesCampo.Form, true, "form.totals.totalPackages", TiposDato.Numero),
                Field("container_number", "Número de contenedor", OrigenesCampo.Manual, false, null,
                    hint: "Lo asigna la agencia de carga."),
            ]);

        yield return Template(
            CodigosDocumento.PhytosanitaryRequest, "Solicitud de certificado fitosanitario",
            TiposDocumento.CertificadoFitosanitario, "SENASA",
            [
                Field("botanical_name", "Nombre botánico", OrigenesCampo.Lot, true, "items[].traceability.species"),
                Field("variety", "Variedad", OrigenesCampo.Lot, true, "items[].traceability.variety"),
                Field("lot_code", "Lote", OrigenesCampo.Lot, true, "items[].traceability.lotCode"),
                Field("crop_year", "Campaña", OrigenesCampo.Lot, true, "items[].traceability.cropYear", TiposDato.Numero),
                Field("origin_province", "Provincia de origen", OrigenesCampo.Organization, true, "organization.province"),
                Field("destination_country", "País de destino", OrigenesCampo.Form, true, "customer.countryName"),
                Field("treatment", "Tratamiento aplicado", OrigenesCampo.Manual, false, null,
                    hint: "Si el país de destino lo exige."),
                Field("additional_declaration", "Declaración adicional", OrigenesCampo.Manual, false, null,
                    hint: "Texto que exija el país de destino."),
            ]);

        yield return Template(
            CodigosDocumento.OriginCertificate, "Certificado de origen",
            TiposDocumento.CertificadoOrigen, "Cámara de Comercio",
            [
                Field("exporter_name", "Exportador", OrigenesCampo.Organization, true, "organization.legalName"),
                Field("consignee", "Consignatario", OrigenesCampo.Customer, true, "customer.name"),
                Field("origin_country", "País de origen", OrigenesCampo.Organization, true, "organization.countryName"),
                Field("destination_country", "País de destino", OrigenesCampo.Form, true, "customer.countryName"),
                Field("total_weight", "Peso total", OrigenesCampo.Form, true, "form.totals.totalKg", TiposDato.Numero),
                Field("tariff_code", "Posición arancelaria", OrigenesCampo.Manual, true, null,
                    hint: "NCM de la semilla de papa."),
            ]);

        yield return Template(
            CodigosDocumento.SeedAnalysisCertificate, "Certificado de análisis de semilla",
            TiposDocumento.AnalisisSemilla, "INASE",
            [
                Field("lot_code", "Lote", OrigenesCampo.Lot, true, "items[].traceability.lotCode"),
                Field("inase_registration", "Registro INASE", OrigenesCampo.Lot, true, "items[].traceability.inaseRegistration"),
                Field("category", "Categoría", OrigenesCampo.Lot, true, "items[].traceability.category"),
                Field("germination_rate", "Poder germinativo", OrigenesCampo.Lot, true, "items[].traceability.germinationRate", TiposDato.Numero),
                Field("purity", "Pureza", OrigenesCampo.Lot, true, "items[].traceability.purity", TiposDato.Numero),
                Field("analysis_date", "Fecha de análisis", OrigenesCampo.Manual, false, null, TiposDato.Fecha,
                    "La del último análisis del lote."),
            ]);

        yield return Template(
            CodigosDocumento.LotLabels, "Rótulos de lote", TiposDocumento.Rotulos, "INASE",
            [
                Field("lot_code", "Lote", OrigenesCampo.Lot, true, "items[].traceability.lotCode"),
                Field("variety", "Variedad", OrigenesCampo.Lot, true, "items[].traceability.variety"),
                Field("category", "Categoría", OrigenesCampo.Lot, true, "items[].traceability.category"),
                Field("net_weight", "Peso neto por bulto", OrigenesCampo.Form, true, "items[].packagingType"),
            ]);
    }

    private static PlantillaDocumento Template(
        string codigo,
        string nombre,
        string tipo,
        string organismo,
        CampoPlantilla[] campos)
    {
        var plantilla = new PlantillaDocumento
        {
            Id = Guid.NewGuid(),
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Organismo = organismo,
            Ambito = AmbitosPlantilla.Formulario,
            Version = 1,
            Activa = true,
            CreatedAt = DateTime.UtcNow,
        };

        var orden = 0;

        foreach (var campo in campos)
        {
            campo.Orden = orden++;
            plantilla.Campos.Add(campo);
        }

        return plantilla;
    }

    private static CampoPlantilla Field(
        string clave,
        string etiqueta,
        string origen,
        bool obligatorio,
        string? reglaMapeo,
        string tipoDato = TiposDato.Texto,
        string? hint = null) => new()
        {
            Id = Guid.NewGuid(),
            Clave = clave,
            Etiqueta = etiqueta,
            Origen = origen,
            Obligatorio = obligatorio,
            ReglaMapeo = reglaMapeo,
            TipoDato = tipoDato,
            Ayuda = hint,
        };
}
