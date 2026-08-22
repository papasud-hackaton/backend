using Microsoft.EntityFrameworkCore;
using Papasur.Application.Auth.Ports;
using Papasur.Domain.Users;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Auth;

public class EfPasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        db.PasswordResetTokens.Update(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        // Marcarlos como usados los deja inutilizables sin borrar el rastro.
        await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), cancellationToken);
    }
}
