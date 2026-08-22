using Microsoft.Extensions.DependencyInjection;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Queries.GetAuditEntries;
using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Application.Auth.Commands.ForgotPassword;
using Papasur.Application.Auth.Commands.Login;
using Papasur.Application.Auth.Commands.Logout;
using Papasur.Application.Auth.Commands.ResetPassword;
using Papasur.Application.Auth.Queries.GetCurrentUser;
using Papasur.Application.Documentos.Commands.ConfirmarDocumento;
using Papasur.Application.Documentos.Commands.GenerarBorrador;
using Papasur.Application.Documentos.Inference;
using Papasur.Application.Documentos.Queries.ObtenerDocumento;
using Papasur.Application.Documentos.Queries.ObtenerPlantillas;
using Papasur.Application.Customers.Commands.CreateCustomer;
using Papasur.Application.Customers.Queries.GetCustomers;
using Papasur.Application.DocumentTypes.Queries.GetDocumentTypes;
using Papasur.Application.ExportForms;
using Papasur.Application.ExportForms.Commands;
using Papasur.Application.ExportForms.Commands.CreateForm;
using Papasur.Application.ExportForms.Commands.GenerateFormDocuments;
using Papasur.Application.ExportForms.Commands.TransitionForm;
using Papasur.Application.ExportForms.Commands.UpdateForm;
using Papasur.Application.ExportForms.Inference;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Application.ExportForms.Queries.GetForms;
using Papasur.Application.Locations.Queries.GetLocations;
using Papasur.Application.Lots.Queries.GetLotById;
using Papasur.Application.Lots.Queries.GetLots;
using Papasur.Application.Metrics.Queries.GetOverview;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Queries.ObtenerItems;
using Papasur.Application.Metrics.Queries.GetMetrics;
using Papasur.Application.Roles.Queries.GetRoles;
using Papasur.Application.Statuses.Queries.GetStatuses;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotes;
using Papasur.Application.Settings.Commands.UpdateOrganization;
using Papasur.Application.Settings.Queries.GetOrganization;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Commands.DeactivateUser;
using Papasur.Application.Users.Commands.SetUserStatus;
using Papasur.Application.Users.Commands.UpdateUser;
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
        services.AddScoped<ICommandHandler<LogoutCommand, Result>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<ForgotPasswordCommand, Result>, ForgotPasswordCommandHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, Result>, ResetPasswordCommandHandler>();
        services.AddScoped<IQueryHandler<GetCurrentUserQuery, Result<UserDto>>, GetCurrentUserQueryHandler>();

        // Users
        services.AddScoped<ICommandHandler<CreateUserCommand, Result<UserDto>>, CreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCommand, Result<UserDto>>, UpdateUserCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateUserCommand, Result<UserDto>>, DeactivateUserCommandHandler>();
        services.AddScoped<ICommandHandler<SetUserStatusCommand, Result<UserDto>>, SetUserStatusCommandHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, PagedResult<UserDto>>, GetUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByIdQuery, Result<UserDto>>, GetUserByIdQueryHandler>();

        // Configuración del exportador
        services.AddScoped<
            IQueryHandler<GetOrganizationQuery, IReadOnlyDictionary<string, string>>,
            GetOrganizationQueryHandler>();
        services.AddScoped<
            ICommandHandler<UpdateOrganizationCommand, Result<IReadOnlyDictionary<string, string>>>,
            UpdateOrganizationCommandHandler>();

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

        // Catálogo del contrato: ubicaciones, clientes y lotes en el shape que consume el front
        services.AddScoped<
            IQueryHandler<GetLocationsQuery, Result<IReadOnlyList<StorageLocationDto>>>,
            GetLocationsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerDto>>>,
            GetCustomersQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateCustomerCommand, Result<CustomerDto>>,
            CreateCustomerCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetLotsQuery, Result<PagedResult<SeedLotDto>>>,
            GetLotsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetLotByIdQuery, Result<SeedLotDto>>,
            GetLotByIdQueryHandler>();

        // Requisitos documentales como dato (contrato §4)
        services.AddScoped<
            IQueryHandler<GetDocumentTypesQuery, Result<IReadOnlyList<DocumentTypeDto>>>,
            GetDocumentTypesQueryHandler>();

        // Formularios de exportación: el núcleo del contrato (§5)
        services.AddScoped<FormAssembler>();
        services.AddScoped<FormItemBuilder>();
        // Datos del exportador: constantes hasta que exista GET /organization.
        services.AddSingleton(OrganizationProfile.Default);
        services.AddScoped<IFormInferenceEngine, FormPathInferenceEngine>();
        services.AddScoped<
            IQueryHandler<GetFormsQuery, Result<PagedResult<ExportFormSummaryDto>>>,
            GetFormsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetFormByIdQuery, Result<ExportFormDto>>,
            GetFormByIdQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateFormCommand, Result<ExportFormDto>>,
            CreateFormCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateFormCommand, Result<UpdateFormResult>>,
            UpdateFormCommandHandler>();
        services.AddScoped<
            ICommandHandler<TransitionFormCommand, Result<ExportFormDto>>,
            TransitionFormCommandHandler>();
        services.AddScoped<
            ICommandHandler<GenerateFormDocumentsCommand, Result<IReadOnlyList<GeneratedDocumentDto>>>,
            GenerateFormDocumentsCommandHandler>();

        // Métricas del tablero (contrato §7)
        services.AddScoped<
            IQueryHandler<GetOverviewQuery, Result<MetricsOverviewResult>>,
            GetOverviewQueryHandler>();

        // Items (feature de ejemplo)
        services.AddScoped<ICommandHandler<CrearItemCommand, Result<Guid>>, CrearItemCommandHandler>();
        services.AddScoped<IQueryHandler<ObtenerItemsQuery, PagedResult<ItemDto>>, ObtenerItemsQueryHandler>();

        return services;
    }
}
