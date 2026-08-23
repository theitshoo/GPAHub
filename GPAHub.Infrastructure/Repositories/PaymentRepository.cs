using GPAHub.Application.Interfaces.Repositories;
using GPAHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GPAHub.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly Persistence.GpaHubDbContext _context;

    public PaymentRepository(Persistence.GpaHubDbContext context)
    {
        _context = context;
    }

    public Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default) =>
        _context.Payments.FirstOrDefaultAsync(p => p.ExternalReference == externalReference, cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await _context.Payments.AddAsync(payment, cancellationToken);
}
