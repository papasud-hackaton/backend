using Microsoft.Extensions.DependencyInjection;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Queries.ObtenerItems;

namespace Papasur.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registro explícito de handlers CQRS (un handler por comando/query, scoped).
    /// Al agregar una feature: definir command/query + handler y registrarlo acá.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Items (feature de ejemplo)
        services.AddScoped<ICommandHandler<CrearItemCommand, Result<Guid>>, CrearItemCommandHandler>();
        services.AddScoped<IQueryHandler<ObtenerItemsQuery, IReadOnlyList<ItemDto>>, ObtenerItemsQueryHandler>();

        return services;
    }
}
