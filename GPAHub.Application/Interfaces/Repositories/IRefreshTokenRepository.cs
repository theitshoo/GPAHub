using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<int> RevokeAllActiveForStudentAsync(Guid studentId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default);
}
