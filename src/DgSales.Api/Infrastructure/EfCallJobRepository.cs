using DgSales.Api.Application;
using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class EfCallJobRepository(SalesDbContext dbContext) : ICallJobRepository
{
    public Task<CallJob?> FindQueuedAsync(Guid leadId, CancellationToken cancellationToken) =>
        dbContext.CallJobs.SingleOrDefaultAsync(x => x.LeadId == leadId && x.Status == CallJobStatus.Queued, cancellationToken);

    public async Task AddAsync(CallJob job, CancellationToken cancellationToken) =>
        await dbContext.CallJobs.AddAsync(job, cancellationToken);
}
