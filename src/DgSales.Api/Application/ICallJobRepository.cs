using DgSales.Api.Domain;

namespace DgSales.Api.Application;

public interface ICallJobRepository
{
    Task<CallJob?> FindQueuedAsync(Guid leadId, CancellationToken cancellationToken);
    Task AddAsync(CallJob job, CancellationToken cancellationToken);
}
