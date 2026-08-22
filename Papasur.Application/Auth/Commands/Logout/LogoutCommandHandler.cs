using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Domain.Audit;

namespace Papasur.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IAuditRepository audit)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await audit.AddAsync(
            AuditFactory.Create(
                command.Actor,
                AuditActions.UserLogout,
                AuditEntityTypes.User,
                command.Actor.Id.ToString()),
            cancellationToken);

        return Result.Success();
    }
}
