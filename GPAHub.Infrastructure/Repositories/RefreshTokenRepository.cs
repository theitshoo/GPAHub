using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public RefreshTokenRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens.AddAsync(token, cancellationToken);

    public async Task<int> RevokeAllActiveForStudentAsync(Guid studentId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.StudentId == studentId && t.RevokedAtUtc == null && t.ExpiresAtUtc > revokedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(revokedAtUtc);
        }

        return activeTokens.Count;
    }
}
