using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Settings.Commands.UpdateOrganization;

/// <summary>Merge parcial: lo que no viene en el body no se toca.</summary>
public sealed record UpdateOrganizationCommand(IReadOnlyDictionary<string, string> Values)
    : ICommand<Result<IReadOnlyDictionary<string, string>>>
{
    public Actor? Actor { get; init; }
}
