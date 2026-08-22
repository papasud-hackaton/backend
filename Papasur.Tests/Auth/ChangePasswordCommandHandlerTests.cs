using Papasur.Application.Abstractions;
using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordResetTokenRepository _tokens = new();
    private readonly FakeAuditRepository _audit = new();
    private readonly User _user;
    private readonly Actor _actor;

    public ChangePasswordCommandHandlerTests()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@papasud.com",
            PasswordHash = new FakePasswordHasher().Hash("Secreta.123"),
            EmployeeId = "A-1042",
            RoleId = RoleIds.Agent,
            Role = new Role { Id = RoleIds.Agent, Name = RoleNames.Agent },
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow,
        };

        _users.Users.Add(_user);
        _actor = new Actor(_user.Id, _user.FullName, RoleNames.Agent);
    }

    private ChangePasswordCommandHandler Handler()
        => new(_users, _tokens, new FakePasswordHasher(), _audit);

    [Fact]
    public async Task Handle_ConLaActualCorrecta_CambiaYAudita()
    {
        var result = await Handler().Handle(
            new ChangePasswordCommand("Secreta.123", "NuevaClave.456") { Actor = _actor },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new FakePasswordHasher().Hash("NuevaClave.456"), _user.PasswordHash);
        Assert.Equal(AuditActions.UserPasswordChanged, Assert.Single(_audit.Entries).Action);
    }

    [Fact]
    public async Task Handle_InvalidaLosEnlacesDeRecuperacionPendientes()
    {
        _tokens.Tokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            TokenHash = "pendiente",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
        });

        await Handler().Handle(
            new ChangePasswordCommand("Secreta.123", "NuevaClave.456") { Actor = _actor },
            CancellationToken.None);

        Assert.NotNull(Assert.Single(_tokens.Tokens).UsedAt);
    }

    [Fact]
    public async Task Handle_ConLaActualIncorrecta_NoCambiaNada()
    {
        var original = _user.PasswordHash;

        var result = await Handler().Handle(
            new ChangePasswordCommand("no-es-la-mia", "NuevaClave.456") { Actor = _actor },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.CurrentPasswordInvalid", result.Error.Code);
        Assert.Equal("La contraseña actual no es correcta.", result.Error.Message);
        Assert.Equal(original, _user.PasswordHash);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Handle_ConLaNuevaCorta_DevuelveFailure()
    {
        var result = await Handler().Handle(
            new ChangePasswordCommand("Secreta.123", "corta") { Actor = _actor },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordTooShort", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ConLaMismaContrasena_DevuelveFailure()
    {
        var result = await Handler().Handle(
            new ChangePasswordCommand("Secreta.123", "Secreta.123") { Actor = _actor },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.PasswordUnchanged", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SinActor_NoHaceNada()
    {
        var result = await Handler().Handle(
            new ChangePasswordCommand("Secreta.123", "NuevaClave.456"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Unauthenticated", result.Error.Code);
    }
}
