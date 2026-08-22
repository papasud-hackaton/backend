using Papasur.Application.Auth.Commands.Login;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeAuditRepository _audit = new();

    private LoginCommandHandler Handler()
        => new(_users, new FakePasswordHasher(), new FakeTokenGenerator(), _audit);

    private User Alta(string status = UserStatuses.Active, string? passwordHash = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@papasud.com",
            PasswordHash = passwordHash ?? new FakePasswordHasher().Hash("Secreta.123"),
            EmployeeId = "A-1042",
            RoleId = RoleIds.Supervisor,
            Role = new Role { Id = RoleIds.Supervisor, Name = RoleNames.Supervisor },
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };

        _users.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task Handle_ConCredencialesValidas_DevuelveUsuarioYToken()
    {
        var user = Alta();

        var result = await Handler().Handle(
            new LoginCommand("Ana@papasud.com", "Secreta.123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"token-for-{user.Email}", result.Value.Token);
        Assert.Equal(user.Id, result.Value.User.Id);
        Assert.Equal(RoleNames.Supervisor, result.Value.User.Role);
        Assert.Equal("Ana", result.Value.User.FirstName);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ActualizaLastLoginYAudita()
    {
        var user = Alta();

        await Handler().Handle(
            new LoginCommand("ana@papasud.com", "Secreta.123") { IpAddress = "10.0.0.7" },
            CancellationToken.None);

        Assert.NotNull(user.LastLoginAt);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.UserLogin, entry.Action);
        Assert.Equal("Ana Pérez", entry.ActorName);
        Assert.Equal(RoleNames.Supervisor, entry.ActorRole);
        Assert.Equal("10.0.0.7", entry.IpAddress);
    }

    [Fact]
    public async Task Handle_UsuarioInexistenteYPasswordIncorrecta_DevuelvenElMISMOError()
    {
        Alta();
        var handler = Handler();

        var inexistente = await handler.Handle(
            new LoginCommand("nadie@papasud.com", "Secreta.123"),
            CancellationToken.None);

        var incorrecta = await handler.Handle(
            new LoginCommand("ana@papasud.com", "otra-cosa"),
            CancellationToken.None);

        // Contrato §1: si difieren, se filtra qué cuentas existen.
        Assert.True(inexistente.IsFailure);
        Assert.True(incorrecta.IsFailure);
        Assert.Equal(inexistente.Error.Code, incorrecta.Error.Code);
        Assert.Equal(inexistente.Error.Message, incorrecta.Error.Message);
        Assert.Equal("El correo o la contraseña no son correctos.", incorrecta.Error.Message);
    }

    [Fact]
    public async Task Handle_UsuarioInactivo_DevuelveElErrorDeCuentaDesactivada()
    {
        Alta(UserStatuses.Inactive);

        var result = await Handler().Handle(
            new LoginCommand("ana@papasud.com", "Secreta.123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Disabled", result.Error.Code);
        Assert.Equal("Tu cuenta está desactivada. Contactá a un administrador.", result.Error.Message);
    }

    [Fact]
    public async Task Handle_UsuarioInvitadoSinContrasena_NoPuedeEntrar()
    {
        Alta(UserStatuses.Invited, passwordHash: string.Empty);

        var result = await Handler().Handle(
            new LoginCommand("ana@papasud.com", "lo-que-sea"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SinDatos_DevuelveFailure()
    {
        var result = await Handler().Handle(new LoginCommand("", ""), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }
}
