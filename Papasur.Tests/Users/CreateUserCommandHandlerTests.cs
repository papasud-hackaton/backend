using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Users;

public class CreateUserCommandHandlerTests
{
    private static (CreateUserCommandHandler Handler, FakeUserRepository Users, FakeAuditRepository Audit) Build()
    {
        var users = new FakeUserRepository();
        var audit = new FakeAuditRepository();
        var handler = new CreateUserCommandHandler(
            users,
            new FakeRoleRepository(),
            new FakePasswordHasher(),
            audit);

        return (handler, users, audit);
    }

    private static CreateUserCommand ValidCommand() =>
        new("Ana Pérez", "Ana.Perez@papasur.com", "Secreta.123", "A-1042", RoleIds.Agente);

    [Fact]
    public async Task Handle_ConDatosValidos_PersisteHasheaYAudita()
    {
        var (handler, users, audit) = Build();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(users.Users);
        Assert.Equal("ana.perez@papasur.com", user.Email);
        Assert.Equal("Ana Pérez", user.Name);
        Assert.Equal("A-1042", user.EmployeeNumber);
        Assert.Equal(RoleIds.Agente, user.RoleId);
        Assert.True(user.IsActive);

        // El handler guarda lo que devuelve el hasher, nunca la contraseña tal cual la mandaron.
        // (que el hash real no contenga la password se verifica en Pbkdf2PasswordHasherTests)
        Assert.NotEqual("Secreta.123", user.PasswordHash);
        Assert.Equal(new FakePasswordHasher().Hash("Secreta.123"), user.PasswordHash);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.UserCreated, entry.Action);
        Assert.Equal(user.Id.ToString(), entry.EntityId);
    }

    [Fact]
    public async Task Handle_AtribuyeLaAuditoriaAQuienEjecuta()
    {
        var (handler, _, audit) = Build();
        var admin = Guid.NewGuid();

        await handler.Handle(ValidCommand() with { PerformedByUserId = admin }, CancellationToken.None);

        Assert.Equal(admin, Assert.Single(audit.Entries).UserId);
    }

    [Fact]
    public async Task Handle_CorreoDuplicado_DevuelveConflictoSinPersistir()
    {
        var (handler, users, _) = Build();
        await handler.Handle(ValidCommand(), CancellationToken.None);

        var result = await handler.Handle(
            ValidCommand() with { EmployeeNumber = "A-9999" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyExists", result.Error.Code);
        Assert.Single(users.Users);
    }

    [Fact]
    public async Task Handle_LegajoDuplicado_DevuelveFailure()
    {
        var (handler, _, _) = Build();
        await handler.Handle(ValidCommand(), CancellationToken.None);

        var result = await handler.Handle(
            ValidCommand() with { Email = "otro@papasur.com" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmployeeNumberAlreadyExists", result.Error.Code);
    }

    [Theory]
    [InlineData("", "User.NameRequired")]
    public async Task Handle_SinNombre_DevuelveFailure(string name, string expectedCode)
    {
        var (handler, users, _) = Build();

        var result = await handler.Handle(ValidCommand() with { Name = name }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Empty(users.Users);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("con espacio@papasur.com")]
    public async Task Handle_CorreoInvalido_DevuelveFailure(string email)
    {
        var (handler, users, _) = Build();

        var result = await handler.Handle(ValidCommand() with { Email = email }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailInvalid", result.Error.Code);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task Handle_PasswordCorta_DevuelveFailure()
    {
        var (handler, users, _) = Build();

        var result = await handler.Handle(ValidCommand() with { Password = "corta" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.PasswordTooShort", result.Error.Code);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task Handle_RolInexistente_DevuelveFailure()
    {
        var (handler, users, _) = Build();

        var result = await handler.Handle(ValidCommand() with { RoleId = 99 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.RoleNotFound", result.Error.Code);
        Assert.Empty(users.Users);
    }
}
