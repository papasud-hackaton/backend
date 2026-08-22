using Papasur.Application.Auth.Commands.Login;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Auth;

public class LoginCommandHandlerTests
{
    private static (LoginCommandHandler Handler, FakeUserRepository Users, FakeAuditRepository Audit) Build(
        bool isActive = true)
    {
        var users = new FakeUserRepository();
        users.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Ana Pérez",
            Email = "ana@papasur.com",
            PasswordHash = new FakePasswordHasher().Hash("Secreta.123"),
            EmployeeNumber = "A-1042",
            RoleId = RoleIds.Supervisor,
            Role = new Role { Id = RoleIds.Supervisor, Name = RoleNames.Supervisor },
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        });

        var audit = new FakeAuditRepository();
        var handler = new LoginCommandHandler(
            users,
            new FakePasswordHasher(),
            new FakeTokenGenerator(),
            audit);

        return (handler, users, audit);
    }

    [Fact]
    public async Task Handle_ConCredencialesValidas_EmiteTokenYAudita()
    {
        var (handler, _, audit) = Build();

        var result = await handler.Handle(
            new LoginCommand("Ana@papasur.com", "Secreta.123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("token-for-ana@papasur.com", result.Value.AccessToken);
        Assert.Equal(RoleNames.Supervisor, result.Value.Role);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(AuditActions.Login, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Handle_ConPasswordIncorrecta_DevuelveErrorGenericoYAuditaElIntento()
    {
        var (handler, _, audit) = Build();

        var result = await handler.Handle(
            new LoginCommand("ana@papasur.com", "otra-cosa") { IpAddress = "10.0.0.7" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.LoginFailed, entry.Action);
        Assert.Equal("10.0.0.7", entry.IpAddress);
    }

    [Fact]
    public async Task Handle_ConCorreoInexistente_NoRevelaSiElUsuarioExiste()
    {
        var (handler, _, audit) = Build();

        var result = await handler.Handle(
            new LoginCommand("nadie@papasur.com", "Secreta.123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Handle_ConUsuarioInactivo_NoEmiteToken()
    {
        var (handler, _, _) = Build(isActive: false);

        var result = await handler.Handle(
            new LoginCommand("ana@papasur.com", "Secreta.123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.UserInactive", result.Error.Code);
    }
}
