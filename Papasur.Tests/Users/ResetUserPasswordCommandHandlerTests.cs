using Papasur.Application.Users.Commands.ResetUserPassword;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Users;

public class ResetUserPasswordCommandHandlerTests
{
    private static (ResetUserPasswordCommandHandler Handler, User User, FakeAuditRepository Audit) Build()
    {
        var users = new FakeUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ana Pérez",
            Email = "ana@papasur.com",
            PasswordHash = new FakePasswordHasher().Hash("Vieja.12345"),
            EmployeeNumber = "A-1042",
            RoleId = RoleIds.Agente,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        users.Users.Add(user);

        var audit = new FakeAuditRepository();

        return (new ResetUserPasswordCommandHandler(users, new FakePasswordHasher(), audit), user, audit);
    }

    [Fact]
    public async Task Handle_ResetaSinPedirLaAnteriorYAuditaAlAdmin()
    {
        var (handler, user, audit) = Build();
        var admin = Guid.NewGuid();

        var result = await handler.Handle(
            new ResetUserPasswordCommand(user.Id, "Nueva.12345") { PerformedByUserId = admin },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new FakePasswordHasher().Hash("Nueva.12345"), user.PasswordHash);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.PasswordReset, entry.Action);
        Assert.Equal(admin, entry.UserId);
        Assert.Equal(user.Id.ToString(), entry.EntityId);
    }

    [Fact]
    public async Task Handle_ConPasswordCorta_DevuelveFailure()
    {
        var (handler, user, _) = Build();
        var original = user.PasswordHash;

        var result = await handler.Handle(
            new ResetUserPasswordCommand(user.Id, "corta"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordTooShort", result.Error.Code);
        Assert.Equal(original, user.PasswordHash);
    }

    [Fact]
    public async Task Handle_ConUsuarioInexistente_DevuelveNotFound()
    {
        var (handler, _, _) = Build();

        var result = await handler.Handle(
            new ResetUserPasswordCommand(Guid.NewGuid(), "Nueva.12345"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }
}
