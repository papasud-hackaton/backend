using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Settings.Ports;
using Papasur.Application.Settings.Queries.GetOrganization;
using Papasur.Domain.Audit;

namespace Papasur.Application.Settings.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandler(
    IOrganizationRepository organization,
    IAuditRepository audit)
    : ICommandHandler<UpdateOrganizationCommand, Result<IReadOnlyDictionary<string, string>>>
{
    public async Task<Result<IReadOnlyDictionary<string, string>>> Handle(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var settings = await organization.GetAsync(cancellationToken);

        var actuales = new Dictionary<string, string>(OrganizationValues.Parse(settings.ValuesJson));

        foreach (var (clave, valor) in command.Values)
        {
            actuales[clave] = valor;
        }

        settings.ValuesJson = OrganizationValues.Serialize(actuales);
        settings.UpdatedAt = DateTime.UtcNow;

        await organization.SaveAsync(settings, cancellationToken);

        if (command.Actor is { } actor)
        {
            await audit.AddAsync(
                AuditFactory.Create(
                    actor,
                    AuditActions.SettingsUpdated,
                    AuditEntityTypes.Settings,
                    settings.Id.ToString(),
                    $"Datos del exportador ({command.Values.Count} campos)."),
                cancellationToken);
        }

        return Result.Success<IReadOnlyDictionary<string, string>>(actuales);
    }
}
