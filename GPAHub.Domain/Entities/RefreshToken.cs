using GPAHub.Domain.Exceptions;

namespace GPAHub.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(Guid studentId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new DomainException("Refresh token expiry must be after creation time.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        TokenHash = tokenHash.Trim();
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsAliveAsOf(DateTimeOffset utcNow) =>
        RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTimeOffset utcNow) => RevokedAtUtc = utcNow;
}
