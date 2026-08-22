using Papasur.Application.Users.Commands.SetUserActive;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Users;

public class SetUserActiveCommandHandlerTests
{
    private static (SetUserActiveCommandHandler Handler, User User, FakeAuditRepository Audit) Build(
        bool isActive = true)
    {
        var users = new FakeUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ana Pérez",
            Email = "ana@papasur.com",
            PasswordHash = "hash",
            EmployeeNumber = "A-1042",
            RoleId = RoleIds.Agente,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
        users.Users.Add(user);

        var audit = new FakeAuditRepository();

        return (new SetUserActiveCommandHandler(users, audit), user, audit);
    }

    [Fact]
    public async Task Handle_DaDeBajaYAudita()
    {
        var (handler, user, audit) = Build();

        var result = await handler.Handle(
            new SetUserActiveCommand(user.Id, false) { PerformedByUserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.Equal(AuditActions.UserDeactivated, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Handle_DaDeAltaYAudita()
    {
        var (handler, user, audit) = Build(isActive: false);

        var result = await handler.Handle(new SetUserActiveCommand(user.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Equal(AuditActions.UserActivated, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Handle_UnAdminNoPuedeDesactivarseASiMismo()
    {
        var (handler, user, audit) = Build();

        var result = await handler.Handle(
            new SetUserActiveCommand(user.Id, false) { PerformedByUserId = user.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.CannotDeactivateSelf", result.Error.Code);
        Assert.True(user.IsActive);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Handle_SinCambioDeEstado_NoAudita()
    {
        var (handler, user, audit) = Build();

        var result = await handler.Handle(new SetUserActiveCommand(user.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Handle_ConUsuarioInexistente_DevuelveNotFound()
    {
        var (handler, _, _) = Build();

        var result = await handler.Handle(
            new SetUserActiveCommand(Guid.NewGuid(), false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }
}
