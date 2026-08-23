using GPAHub.Domain.Entities;

namespace GPAHub.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
