using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papasur.Domain.Documentos;
using Papasur.Domain.Inventory;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// Siembra datos de trazabilidad de DEMO (idempotente: sólo si no hay lotes) extraídos de la planilla
/// de movimientos real (hojas "De campo a Frío" y "Env a Frío") y del plano Santa Ana – Marisol, más
/// una plantilla de proforma con sus reglas de mapeo. Deja el sistema listo para generar documentos
/// end-to-end sin depender de un archivo externo. No usar como carga productiva.
/// </summary>
public sealed class TrazabilidadSeeder(AppDbContext db, ILogger<TrazabilidadSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Las ubicaciones son catálogo, no datos de demo: van aunque los lotes ya estén cargados.
        await SeedUbicacionesAsync(cancellationToken);

        if (await db.Lotes.AnyAsync(cancellationToken))
        {
            // Ya hay trazabilidad cargada (importada o de una corrida anterior): sólo se completan
            // los datos que las columnas nuevas dejaron vacíos. Nunca se pisa lo que ya tiene valor.
            await BackfillAsync(cancellationToken);
            return;
        }

        // --- Variedades ---
        var agata = Variedad("agata");
        var spunta = Variedad("spunta");
        var kingRusset = Variedad("king russet");
        var asterix = Variedad("asterix");
        db.Variedades.AddRange(agata, spunta, kingRusset, asterix);

        // --- Campo de origen (plano Santa Ana – Marisol, pivote B) ---
        var marisol = new Campo
        {
            Id = Guid.NewGuid(),
            Nombre = "Marisol",
            Establecimiento = "Santa Ana",
            Pivote = "B",
            Cuadrante = "6",
        };
        db.Campos.Add(marisol);

        // --- Transportistas ---
        var serantes = Transportista("serantes-vera");
        var camilo = Transportista("Camilo Gastón");
        var arenas = Transportista("Alvaro Arenas");
        var cerone = Transportista("Cerone (Raphael)");
        db.Transportistas.AddRange(serantes, camilo, arenas, cerone);

        // --- Ubicaciones (ya sembradas arriba) ---
        var ubicaciones = await db.StorageLocations.ToDictionaryAsync(u => u.Code, cancellationToken);
        var cr01 = ubicaciones["CR-01"];
        var cr02 = ubicaciones["CR-02"];
        var cr03 = ubicaciones["CR-03"];
        var wh01 = ubicaciones["WH-01"];

        // --- Clientes / destinos comerciales ---
        // Los datos fiscales y de domicilio son de relleno hasta que Papasud los entregue: sin
        // ellos la proforma no se puede emitir, y el sistema tiene que poder decirlo.
        var dospanca = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = "Dospanca",
            Pais = "Brasil",
            CountryCode = "BR",
            City = "Curitiba",
            DefaultIncoterm = "FOB",
            DefaultPortOfDischarge = "Paranaguá",
        };

        var wemar = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = "Wemar - Mc Cain",
            Pais = "Brasil",
            CountryCode = "BR",
            City = "Araucária",
            DefaultIncoterm = "FOB",
            DefaultPortOfDischarge = "Paranaguá",
        };

        db.Clientes.AddRange(dospanca, wemar);

        // --- Lotes + movimientos (subconjunto real de la planilla) ---
        var lote241 = Lote("241", agata, marisol, "fiscalizada", 13.0m, cr01);
        lote241.Movimientos.Add(Mov(TiposMovimiento.CampoAFrio, "1001", new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
            35160m, 705, 49.87m, "bolsa", null, "sin tamañar", serantes, dospanca, "dospanca",
            "13354667-7", "b.roja sin tamañar"));
        lote241.Movimientos.Add(Mov(TiposMovimiento.CampoAFrio, "1002", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            34500m, 700, 49.28m, "bolsa", null, "sin tamañar", serantes, dospanca, "dospanca",
            "13359963-0", "b.roja y blancas s/tamañar"));

        var lote224 = Lote("224", agata, marisol, "fiscalizada", 13.0m, cr01);
        lote224.Movimientos.Add(Mov(TiposMovimiento.EnvioFrio, "805", new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc),
            29120m, 568, 51.26m, "bolsa", null, "exportacion", arenas, dospanca, "dospanca",
            "13250335-4", "bolsa blanca-hilo negro"));
        lote224.Movimientos.Add(Mov(TiposMovimiento.EnvioFrio, "808", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            13970m, 265, 52.71m, "bolsa", null, "exportacion", arenas, dospanca, "dospanca",
            "13258734-5", "bolsa blanca-hilo negro"));

        // "afectada" en las observaciones del remito: el lote queda bloqueado para despacho.
        var lote300 = Lote("300", spunta, marisol, "fiscalizada", 6.9m, cr02, enCuarentena: true);
        lote300.Movimientos.Add(Mov(TiposMovimiento.CampoAFrio, "1006", new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            35980m, 700, 51.40m, "bolsa", null, "exportacion", camilo, dospanca, "dospanca",
            "13451462-0", "b.blanca - afectada"));

        var lote910 = Lote("910", kingRusset, marisol, "fiscalizada", 0.18m, wh01);
        lote910.Movimientos.Add(Mov(TiposMovimiento.EntregaCliente, "674", new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            28380m, 579, 49.0m, "granel", "granel", "recibo", camilo, wemar, "wemar-mc cain",
            "13374249-2", "directo de santa ana"));

        var lote821 = Lote("821", asterix, marisol, "fiscalizada", 4.7m, cr03);
        lote821.Movimientos.Add(Mov(TiposMovimiento.EnvioFrio, "820", new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            30460m, 600, 50.76m, "bolsa", null, "exportacion", cerone, dospanca, "dospanca",
            "13312627-9", "bolsa verde-hilo negro"));

        db.Lotes.AddRange(lote241, lote224, lote300, lote910, lote821);

        // --- Plantilla de proforma (requisitos como dato + reglas de mapeo para inferencia) ---
        db.PlantillasDocumento.Add(ProformaExportacion());

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Trazabilidad de demo sembrada: {Lotes} lotes, {Movimientos} movimientos y la plantilla de proforma.",
            5,
            8);
    }

    /// <summary>Las cuatro ubicaciones del brief: 3 cámaras de frío y el galpón. Idempotente.</summary>
    private async Task SeedUbicacionesAsync(CancellationToken cancellationToken)
    {
        if (await db.StorageLocations.AnyAsync(cancellationToken))
        {
            return;
        }

        db.StorageLocations.AddRange(
            Ubicacion("CR-01", "Cámara 1 — Santa Ana", LocationTypes.ColdRoom, 4.0m),
            Ubicacion("CR-02", "Cámara 2 — Santa Ana", LocationTypes.ColdRoom, 4.0m),
            Ubicacion("CR-03", "Cámara 3 — Marisol", LocationTypes.ColdRoom, 3.5m),
            Ubicacion("WH-01", "Galpón central", LocationTypes.Warehouse, null));

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Ubicaciones de stock sembradas: 3 cámaras + galpón.");
    }

    /// <summary>
    /// Completa lo que las columnas nuevas dejaron vacío en datos ya cargados: ubicación del lote,
    /// campaña, cuarentena y los datos de país del cliente. Sólo escribe donde no hay valor.
    /// </summary>
    private async Task BackfillAsync(CancellationToken cancellationToken)
    {
        var cambios = 0;

        var ubicaciones = await db.StorageLocations.ToDictionaryAsync(u => u.Code, cancellationToken);

        // Reparto conocido de la planilla; lo que no esté acá queda en el galpón.
        var porLote = new Dictionary<string, string>
        {
            ["241"] = "CR-01",
            ["224"] = "CR-01",
            ["300"] = "CR-02",
            ["821"] = "CR-03",
            ["910"] = "WH-01",
        };

        var lotes = await db.Lotes
            .Include(l => l.Movimientos)
            .Where(l => l.StorageLocationId == null || l.Campania == null)
            .ToListAsync(cancellationToken);

        foreach (var lote in lotes)
        {
            if (lote.StorageLocationId is null)
            {
                var code = porLote.GetValueOrDefault(lote.Codigo, "WH-01");

                if (ubicaciones.TryGetValue(code, out var ubicacion))
                {
                    lote.StorageLocationId = ubicacion.Id;
                    cambios++;
                }
            }

            if (lote.Campania is null && lote.Movimientos.Count > 0)
            {
                lote.Campania = lote.Movimientos.Min(m => m.Fecha).Year;
                cambios++;
            }

            // "afectada" en las observaciones del remito: el lote no puede despacharse.
            if (!lote.EnCuarentena && lote.Movimientos.Any(m =>
                    m.Observaciones != null && m.Observaciones.Contains("afectada", StringComparison.OrdinalIgnoreCase)))
            {
                lote.EnCuarentena = true;
                cambios++;
            }
        }

        var clientes = await db.Clientes.Where(c => c.CountryCode == null).ToListAsync(cancellationToken);

        foreach (var cliente in clientes)
        {
            // El país ya estaba como texto; se deriva el código ISO que exigen los documentos.
            cliente.CountryCode = cliente.Pais switch
            {
                "Brasil" => "BR",
                "Paraguay" => "PY",
                "Uruguay" => "UY",
                "Argentina" => "AR",
                _ => null,
            };

            if (cliente.CountryCode is not null)
            {
                cambios++;
            }
        }

        if (cambios == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Trazabilidad existente completada: {Cambios} campos nuevos rellenados.", cambios);
    }

    private static Variedad Variedad(string nombre) => new() { Id = Guid.NewGuid(), Nombre = nombre };

    private static Transportista Transportista(string nombre) => new() { Id = Guid.NewGuid(), Nombre = nombre };

    private static StorageLocation Ubicacion(string code, string name, string type, decimal? temperatureC) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Type = type,
        TemperatureC = temperatureC,
    };

    private static Lote Lote(
        string codigo,
        Variedad variedad,
        Campo campo,
        string categoria,
        decimal superficieHa,
        StorageLocation ubicacion,
        bool enCuarentena = false) => new()
    {
        Id = Guid.NewGuid(),
        Codigo = codigo,
        Variedad = variedad,
        Campo = campo,
        Categoria = categoria,
        SuperficieHa = superficieHa,
        StorageLocation = ubicacion,
        // Campaña de los movimientos importados. Calidad (germinación, pureza, INASE) todavía no
        // vino: se deja vacía a propósito, y el sistema lo advierte en vez de inventarla.
        Campania = 2026,
        EnCuarentena = enCuarentena,
        CreatedAt = DateTime.UtcNow,
    };

    private static Movimiento Mov(
        string tipo, string remito, DateTime fecha, decimal kg, int? bolsas, decimal? kgProm,
        string? presentacion, string? categoria, string? calibre, Transportista transportista,
        Cliente cliente, string? destino, string? dtv, string? observaciones) => new()
    {
        Id = Guid.NewGuid(),
        Tipo = tipo,
        NumeroRemito = remito,
        Fecha = fecha,
        Kilogramos = kg,
        Bolsas = bolsas,
        KgPromedio = kgProm,
        Presentacion = presentacion,
        Categoria = categoria,
        Calibre = calibre,
        Transportista = transportista,
        Cliente = cliente,
        Destino = destino,
        Dtv = dtv,
        Observaciones = observaciones,
    };

    private static PlantillaDocumento ProformaExportacion()
    {
        var plantilla = new PlantillaDocumento
        {
            Id = Guid.NewGuid(),
            Codigo = "proforma_lote",
            Nombre = "Proforma de exportación de semilla",
            Tipo = TiposDocumento.Proforma,
            // Ámbito lote: la usa el copiloto sobre un despacho concreto, no sobre un envío.
            Ambito = AmbitosPlantilla.Lote,
            Organismo = "SENASA",
            PaisDestino = "Brasil",
            Version = 1,
            Activa = true,
            CreatedAt = DateTime.UtcNow,
        };

        var orden = 0;
        void Campo(string clave, string etiqueta, string tipoDato, bool obligatorio, string? reglaMapeo)
            => plantilla.Campos.Add(new CampoPlantilla
            {
                Id = Guid.NewGuid(),
                Clave = clave,
                Etiqueta = etiqueta,
                TipoDato = tipoDato,
                Obligatorio = obligatorio,
                ReglaMapeo = reglaMapeo,
                Orden = orden++,
            });

        // Campos inferibles desde la trazabilidad (regla de mapeo != null)...
        Campo("lote", "Lote", TiposDato.Texto, true, "lote.codigo");
        Campo("variedad", "Variedad", TiposDato.Texto, true, "lote.variedad");
        Campo("campo_origen", "Campo de origen", TiposDato.Texto, false, "lote.campo");
        Campo("categoria", "Categoría / calibre", TiposDato.Texto, false, "movimiento.calibre");
        Campo("remito", "Remito", TiposDato.Texto, false, "movimiento.numero_remito");
        Campo("fecha", "Fecha de despacho", TiposDato.Fecha, false, "movimiento.fecha");
        Campo("peso_neto_kg", "Peso neto (kg)", TiposDato.Numero, true, "movimiento.kilogramos");
        Campo("bolsas", "Cantidad de bolsas", TiposDato.Numero, false, "movimiento.bolsas");
        Campo("kg_por_bolsa", "Kg por bolsa", TiposDato.Numero, false, "movimiento.kg_promedio");
        Campo("transportista", "Transportista", TiposDato.Texto, false, "movimiento.transportista");
        Campo("cliente", "Cliente / importador", TiposDato.Texto, true, "movimiento.cliente");
        Campo("pais_destino", "País de destino", TiposDato.Texto, true, "movimiento.pais");
        Campo("dtv", "DTV", TiposDato.Texto, true, "movimiento.dtv");

        // ...y campos que sólo puede completar la persona (sin regla: quedan pendientes/manual/dictado).
        Campo("exportador", "Exportador", TiposDato.Texto, true, null);
        Campo("precio_unitario_usd", "Precio unitario (USD/kg)", TiposDato.Numero, true, null);
        Campo("incoterm", "Incoterm", TiposDato.Texto, false, null);
        Campo("observaciones", "Observaciones", TiposDato.Texto, false, "movimiento.observaciones");

        return plantilla;
    }
}
