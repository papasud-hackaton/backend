namespace Papasur.Domain.Statuses;

/// <summary>
/// Catálogo de estados (tabla fija sembrada por migración: en proceso, finalizado, cancelado).
/// Las entidades que tengan ciclo de vida (documento, proforma, lote) referencian esta tabla
/// con una FK StatusId.
/// </summary>
public class Status
{
    public int Id { get; set; }

    /// <summary>Código estable para el código y la API (snake_case, no cambia).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Etiqueta para mostrar al usuario.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Códigos e IDs fijos del catálogo de estados.</summary>
public static class StatusCodes
{
    public const string EnProceso = "en_proceso";

    public const string Finalizado = "finalizado";

    public const string Cancelado = "cancelado";

    public static readonly string[] All = [EnProceso, Finalizado, Cancelado];

    public static bool Exists(string code) => All.Contains(code);
}

public static class StatusIds
{
    public const int EnProceso = 1;

    public const int Finalizado = 2;

    public const int Cancelado = 3;
}
