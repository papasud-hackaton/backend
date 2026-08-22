using Microsoft.Extensions.DependencyInjection;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Queries.GetAuditEntries;
using Papasur.Application.Auth.Commands.Login;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Queries.ObtenerItems;
using Papasur.Application.Metrics.Queries.GetMetrics;
using Papasur.Application.Roles.Queries.GetRoles;
using Papasur.Application.Statuses.Queries.GetStatuses;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registro explícito de handlers CQRS (un handler por comando/query, scoped).
    /// Al agregar una feature: definir command/query + handler y registrarlo acá.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<ICommandHandler<LoginCommand, Result<LoginResponse>>, LoginCommandHandler>();

        // Users
        services.AddScoped<ICommandHandler<CreateUserCommand, Result<Guid>>, CreateUserCommandHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, PagedResult<UserDto>>, GetUsersQueryHandler>();

        // Roles (catálogo)
        services.AddScoped<IQueryHandler<GetRolesQuery, PagedResult<RoleDto>>, GetRolesQueryHandler>();

        // Statuses (catálogo)
        services.AddScoped<IQueryHandler<GetStatusesQuery, PagedResult<StatusDto>>, GetStatusesQueryHandler>();

        // Auditoría
        services.AddScoped<
            IQueryHandler<GetAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>>,
            GetAuditEntriesQueryHandler>();

        // Métricas (genéricas: recorren todos los IMetricProvider registrados)
        services.AddScoped<
            IQueryHandler<GetMetricsQuery, Result<PagedResult<MetricDto>>>,
            GetMetricsQueryHandler>();

        // Items (feature de ejemplo)
        services.AddScoped<ICommandHandler<CrearItemCommand, Result<Guid>>, CrearItemCommandHandler>();
        services.AddScoped<IQueryHandler<ObtenerItemsQuery, PagedResult<ItemDto>>, ObtenerItemsQueryHandler>();

        return services;
    }
}
