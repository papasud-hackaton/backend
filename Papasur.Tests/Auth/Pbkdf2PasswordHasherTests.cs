using Papasur.Infrastructure.Auth;

namespace Papasur.Tests.Auth;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NoGuardaLaPasswordEnClaroYUsaSaltPorUsuario()
    {
        var a = _hasher.Hash("Secreta.123");
        var b = _hasher.Hash("Secreta.123");

        Assert.DoesNotContain("Secreta.123", a, StringComparison.Ordinal);
        Assert.NotEqual(a, b); // salt distinto por hash
        Assert.Equal(3, a.Split('.').Length);
    }

    [Fact]
    public void Verify_AceptaLaPasswordCorrectaYRechazaLaIncorrecta()
    {
        var hash = _hasher.Hash("Secreta.123");

        Assert.True(_hasher.Verify("Secreta.123", hash));
        Assert.False(_hasher.Verify("secreta.123", hash));
        Assert.False(_hasher.Verify("otra", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("formato-invalido")]
    [InlineData("no-numero.c2FsdA==.aGFzaA==")]
    public void Verify_ConHashInvalido_DevuelveFalseSinExplotar(string hash)
        => Assert.False(_hasher.Verify("Secreta.123", hash));
}
