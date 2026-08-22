using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private static (ChangePasswordCommandHandler Handler, User User, FakeUserRepository Users, FakeAuditRepository Audit)
        Build()
    {
        var users = new FakeUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ana Pérez",
            Email = "ana@papasur.com",
            PasswordHash = new FakePasswordHasher().Hash("Secreta.123"),
            EmployeeNumber = "A-1042",
            RoleId = RoleIds.Agente,
            Role = new Role { Id = RoleIds.Agente, Name = RoleNames.Agente },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        users.Users.Add(user);

        var audit = new FakeAuditRepository();

        return (new ChangePasswordCommandHandler(users, new FakePasswordHasher(), audit), user, users, audit);
    }

    [Fact]
    public async Task Handle_ConPasswordActualCorrecta_CambiaYAudita()
    {
        var (handler, user, _, audit) = Build();

        var result = await handler.Handle(
            new ChangePasswordCommand("Secreta.123", "NuevaClave.456") { UserId = user.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new FakePasswordHasher().Hash("NuevaClave.456"), user.PasswordHash);
        Assert.Equal(AuditActions.PasswordChanged, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Handle_ConPasswordActualIncorrecta_NoCambiaNada()
    {
        var (handler, user, _, audit) = Build();
        var original = user.PasswordHash;

        var result = await handler.Handle(
            new ChangePasswordCommand("no-es-la-mia", "NuevaClave.456") { UserId = user.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.CurrentPasswordInvalid", result.Error.Code);
        Assert.Equal(original, user.PasswordHash);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Handle_ConPasswordNuevaCorta_DevuelveFailure()
    {
        var (handler, user, _, _) = Build();

        var result = await handler.Handle(
            new ChangePasswordCommand("Secreta.123", "corta") { UserId = user.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordTooShort", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ConLaMismaPassword_DevuelveFailure()
    {
        var (handler, user, _, _) = Build();

        var result = await handler.Handle(
            new ChangePasswordCommand("Secreta.123", "Secreta.123") { UserId = user.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.PasswordUnchanged", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ConUsuarioInexistente_DevuelveNotFound()
    {
        var (handler, _, _, _) = Build();

        var result = await handler.Handle(
            new ChangePasswordCommand("Secreta.123", "NuevaClave.456") { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }
}
