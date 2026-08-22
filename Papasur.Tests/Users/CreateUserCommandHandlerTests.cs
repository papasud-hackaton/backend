using Papasur.Application.Abstractions;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Users;

public class CreateUserCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordResetTokenRepository _tokens = new();
    private readonly FakeInvitationSender _sender = new();
    private readonly FakeAuditRepository _audit = new();

    private CreateUserCommandHandler Handler()
        => new(_users, new FakeRoleRepository(), _tokens, _sender, _audit);

    private static CreateUserCommand Valido() =>
        new("Ana", "Pérez", "Ana.Perez@papasud.com", "A-1042", RoleNames.Agent, "+54 9 11 5555");

    [Fact]
    public async Task Handle_CreaElUsuarioComoInvitadoYSinContrasena()
    {
        var result = await Handler().Handle(Valido(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(_users.Users);

        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("Pérez", user.LastName);
        Assert.Equal("ana.perez@papasud.com", user.Email);
        Assert.Equal("A-1042", user.EmployeeId);
        Assert.Equal(RoleIds.Agent, user.RoleId);
        // Contrato §2: nace invitado y sin contraseña; la define desde la invitación.
        Assert.Equal(UserStatuses.Invited, user.Status);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Handle_EmiteLaInvitacionConUnTokenGuardadoHasheado()
    {
        await Handler().Handle(Valido(), CancellationToken.None);

        var (email, token, esInvitacion) = Assert.Single(_sender.Enviados);
        Assert.Equal("ana.perez@papasud.com", email);
        Assert.True(esInvitacion);

        var guardado = Assert.Single(_tokens.Tokens);
        // El token en claro sólo viaja en el enlace: en la base va su hash.
        Assert.NotEqual(token, guardado.TokenHash);
        Assert.True(guardado.ExpiresAt > DateTime.UtcNow);
        Assert.Null(guardado.UsedAt);
    }

    [Fact]
    public async Task Handle_DevuelveElUsuarioSinExponerElHash()
    {
        var result = await Handler().Handle(Valido(), CancellationToken.None);

        var dto = result.Value;
        Assert.Equal("Ana", dto.FirstName);
        Assert.Equal(RoleNames.Agent, dto.Role);
        Assert.Equal(UserStatuses.Invited, dto.Status);
        Assert.Equal("+54 9 11 5555", dto.Phone);
    }

    [Fact]
    public async Task Handle_AuditaElAltaConElActor()
    {
        var actor = new Actor(Guid.NewGuid(), "Admin Papasud", RoleNames.Admin, "10.0.0.7");

        await Handler().Handle(Valido() with { Actor = actor }, CancellationToken.None);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.UserCreated, entry.Action);
        Assert.Equal(actor.Id, entry.UserId);
        // La auditoría guarda el nombre y el rol del actor, no una FK.
        Assert.Equal("Admin Papasud", entry.ActorName);
        Assert.Equal(RoleNames.Admin, entry.ActorRole);
        Assert.Equal("10.0.0.7", entry.IpAddress);
    }

    [Fact]
    public async Task Handle_CorreoDuplicado_NoCrea()
    {
        await Handler().Handle(Valido(), CancellationToken.None);

        var result = await Handler().Handle(
            Valido() with { EmployeeId = "A-9999" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyExists", result.Error.Code);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Handle_LegajoDuplicado_NoCrea()
    {
        await Handler().Handle(Valido(), CancellationToken.None);

        var result = await Handler().Handle(
            Valido() with { Email = "otra@papasud.com" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmployeeIdAlreadyExists", result.Error.Code);
        Assert.Single(_users.Users);
    }

    [Theory]
    [InlineData("", "Pérez", "User.FirstNameRequired")]
    [InlineData("Ana", "", "User.LastNameRequired")]
    public async Task Handle_SinNombreOApellido_DevuelveFailure(string nombre, string apellido, string code)
    {
        var result = await Handler().Handle(
            Valido() with { FirstName = nombre, LastName = apellido },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Empty(_users.Users);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("con espacio@papasud.com")]
    public async Task Handle_CorreoInvalido_DevuelveFailure(string email)
    {
        var result = await Handler().Handle(Valido() with { Email = email }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailInvalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RolInexistente_DevuelveFailure()
    {
        var result = await Handler().Handle(Valido() with { Role = "gerente" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.RoleNotFound", result.Error.Code);
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Handle_SinLegajo_DevuelveFailure()
    {
        var result = await Handler().Handle(Valido() with { EmployeeId = "  " }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmployeeIdRequired", result.Error.Code);
    }
}
