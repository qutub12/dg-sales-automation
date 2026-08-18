using DgSales.Api.Domain;

namespace DgSales.Api.Application;

public interface ILeadRepository
{
    Task AddAsync(Lead lead, CancellationToken cancellationToken);
    Task<Lead?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Lead?> FindByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
