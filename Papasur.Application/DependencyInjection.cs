using Microsoft.Extensions.DependencyInjection;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Queries.GetAuditEntries;
using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Application.Auth.Commands.Login;
using Papasur.Application.Auth.Queries.GetCurrentUser;
using Papasur.Application.Documentos.Commands.ConfirmarDocumento;
using Papasur.Application.Documentos.Commands.GenerarBorrador;
using Papasur.Application.Documentos.Inference;
using Papasur.Application.Documentos.Queries.ObtenerDocumento;
using Papasur.Application.Documentos.Queries.ObtenerPlantillas;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Queries.ObtenerItems;
using Papasur.Application.Metrics.Queries.GetMetrics;
using Papasur.Application.Roles.Queries.GetRoles;
using Papasur.Application.Statuses.Queries.GetStatuses;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotes;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Commands.ResetUserPassword;
using Papasur.Application.Users.Commands.SetUserActive;
using Papasur.Application.Users.Queries.GetUserById;
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
        services.AddScoped<ICommandHandler<ChangePasswordCommand, Result>, ChangePasswordCommandHandler>();
        services.AddScoped<IQueryHandler<GetCurrentUserQuery, Result<UserDto>>, GetCurrentUserQueryHandler>();

        // Users
        services.AddScoped<ICommandHandler<CreateUserCommand, Result<Guid>>, CreateUserCommandHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, PagedResult<UserDto>>, GetUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByIdQuery, Result<UserDto>>, GetUserByIdQueryHandler>();
        services.AddScoped<ICommandHandler<ResetUserPasswordCommand, Result>, ResetUserPasswordCommandHandler>();
        services.AddScoped<ICommandHandler<SetUserActiveCommand, Result>, SetUserActiveCommandHandler>();

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

        // Trazabilidad (lotes + movimientos importados de la planilla)
        services.AddScoped<
            IQueryHandler<ObtenerLotesQuery, PagedResult<LoteDto>>,
            ObtenerLotesQueryHandler>();
        services.AddScoped<
            IQueryHandler<ObtenerLotePorIdQuery, Result<LoteDetalleDto>>,
            ObtenerLotePorIdQueryHandler>();

        // Documentos de exportación (copiloto)
        // Motor de inferencia por reglas (determinístico, sin dependencias externas).
        services.AddScoped<IMotorInferencia, MotorInferenciaReglas>();
        services.AddScoped<
            IQueryHandler<ObtenerPlantillasQuery, PagedResult<PlantillaDto>>,
            ObtenerPlantillasQueryHandler>();
        services.AddScoped<
            ICommandHandler<GenerarBorradorCommand, Result<Guid>>,
            GenerarBorradorCommandHandler>();
        services.AddScoped<
            IQueryHandler<ObtenerDocumentoQuery, Result<DocumentoExportacionDto>>,
            ObtenerDocumentoQueryHandler>();
        services.AddScoped<
            ICommandHandler<ConfirmarDocumentoCommand, Result>,
            ConfirmarDocumentoCommandHandler>();

        // Items (feature de ejemplo)
        services.AddScoped<ICommandHandler<CrearItemCommand, Result<Guid>>, CrearItemCommandHandler>();
        services.AddScoped<IQueryHandler<ObtenerItemsQuery, PagedResult<ItemDto>>, ObtenerItemsQueryHandler>();

        return services;
    }
}
