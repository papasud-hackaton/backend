using Papasur.Application.Documentos.Inference;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Tests.Documentos;

/// <summary>
/// El motor es el corazón del copiloto: define qué se pre-completa solo y qué queda para la persona.
/// Estos tests fijan ese contrato regla por regla.
/// </summary>
public class MotorInferenciaReglasTests
{
    private readonly MotorInferenciaReglas _motor = new();

    private static (Lote Lote, Movimiento Movimiento) Trazabilidad()
    {
        var lote = new Lote
        {
            Id = Guid.NewGuid(),
            Codigo = "224",
            Variedad = new Variedad { Id = Guid.NewGuid(), Nombre = "agata" },
            Campo = new Campo
            {
                Id = Guid.NewGuid(),
                Nombre = "Marisol",
                Establecimiento = "Santa Ana",
                Pivote = "B",
            },
            Categoria = "exportacion",
            SuperficieHa = 12.5m,
        };

        var movimiento = new Movimiento
        {
            Id = Guid.NewGuid(),
            LoteId = lote.Id,
            Tipo = TiposMovimiento.EntregaCliente,
            NumeroRemito = "805",
            Fecha = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc),
            Kilogramos = 29120m,
            Bolsas = 568,
            KgPromedio = 51.26m,
            Presentacion = "bolsa",
            Categoria = "exportacion",
            Calibre = "exportacion",
            Transportista = new Transportista { Id = Guid.NewGuid(), Nombre = "Alvaro Arenas" },
            Cliente = new Cliente { Id = Guid.NewGuid(), Nombre = "Dospanca", Pais = "Brasil" },
            Comisionista = "Juan Comisionista",
            Destino = "Brasil",
            Dtv = "13250335-4",
        };

        lote.Movimientos.Add(movimiento);

        return (lote, movimiento);
    }

    [Theory]
    [InlineData("lote.codigo", "224")]
    [InlineData("lote.variedad", "agata")]
    [InlineData("lote.campo", "Marisol")]
    [InlineData("lote.establecimiento", "Santa Ana")]
    [InlineData("lote.categoria", "exportacion")]
    [InlineData("lote.superficie_ha", "12.5")]
    [InlineData("movimiento.numero_remito", "805")]
    [InlineData("movimiento.fecha", "2026-03-07")]
    [InlineData("movimiento.kilogramos", "29120")]
    [InlineData("movimiento.bolsas", "568")]
    [InlineData("movimiento.kg_promedio", "51.26")]
    [InlineData("movimiento.presentacion", "bolsa")]
    [InlineData("movimiento.categoria", "exportacion")]
    [InlineData("movimiento.calibre", "exportacion")]
    [InlineData("movimiento.transportista", "Alvaro Arenas")]
    [InlineData("movimiento.cliente", "Dospanca")]
    [InlineData("movimiento.pais", "Brasil")]
    [InlineData("movimiento.comisionista", "Juan Comisionista")]
    [InlineData("movimiento.destino", "Brasil")]
    [InlineData("movimiento.dtv", "13250335-4")]
    public void Inferir_ResuelveCadaReglaDeMapeo(string regla, string esperado)
    {
        var (lote, movimiento) = Trazabilidad();

        var resultado = _motor.Inferir(regla, lote, movimiento);

        Assert.NotNull(resultado);
        Assert.Equal(esperado, resultado.Valor);
        // La traza de dónde salió el dato es lo que hace auditable la inferencia.
        Assert.Equal(regla, resultado.InferidoDesde);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Inferir_SinRegla_DevuelveNull(string? regla)
    {
        var (lote, movimiento) = Trazabilidad();

        Assert.Null(_motor.Inferir(regla, lote, movimiento));
    }

    [Fact]
    public void Inferir_ConReglaDesconocida_DevuelveNull()
    {
        var (lote, movimiento) = Trazabilidad();

        Assert.Null(_motor.Inferir("movimiento.inventada", lote, movimiento));
    }

    [Fact]
    public void Inferir_ReglaDeMovimientoSinMovimiento_DevuelveNull()
    {
        var (lote, _) = Trazabilidad();

        // Sin movimiento elegido, lo del lote sigue infiriéndose y lo del movimiento queda para el humano.
        Assert.Equal("224", _motor.Inferir("lote.codigo", lote, null)?.Valor);
        Assert.Null(_motor.Inferir("movimiento.dtv", lote, null));
    }

    [Fact]
    public void Inferir_NormalizaMayusculasYEspacios()
    {
        var (lote, movimiento) = Trazabilidad();

        var resultado = _motor.Inferir("  MOVIMIENTO.DTV  ", lote, movimiento);

        Assert.Equal("13250335-4", resultado?.Valor);
        Assert.Equal("movimiento.dtv", resultado?.InferidoDesde);
    }

    [Fact]
    public void Inferir_CampoSinDato_DevuelveNullEnVezDeVacio()
    {
        var (lote, movimiento) = Trazabilidad();
        movimiento.Dtv = null;
        movimiento.Calibre = "   ";
        lote.SuperficieHa = null;

        Assert.Null(_motor.Inferir("movimiento.dtv", lote, movimiento));
        Assert.Null(_motor.Inferir("movimiento.calibre", lote, movimiento));
        Assert.Null(_motor.Inferir("lote.superficie_ha", lote, movimiento));
    }

    [Fact]
    public void Inferir_FormateaNumerosSinDependerDeLaCulturaLocal()
    {
        var (lote, movimiento) = Trazabilidad();
        movimiento.KgPromedio = 1234.5678m;

        // Punto decimal siempre: el documento no puede depender de la config regional del server.
        Assert.Equal("1234.568", _motor.Inferir("movimiento.kg_promedio", lote, movimiento)?.Valor);
    }
}
