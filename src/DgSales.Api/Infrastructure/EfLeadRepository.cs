using DgSales.Api.Application;
using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class EfLeadRepository(SalesDbContext dbContext) : ILeadRepository
{
    public async Task AddAsync(Lead lead, CancellationToken cancellationToken) =>
        await dbContext.Leads.AddAsync(lead, cancellationToken);

    public Task<Lead?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Leads.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Lead?> FindByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        var normalized = Lead.NormalizePhone(phone);
        return dbContext.Leads.SingleOrDefaultAsync(x => x.Phone == normalized, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Lead>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Leads.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
