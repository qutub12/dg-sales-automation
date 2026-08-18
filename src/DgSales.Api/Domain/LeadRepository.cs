using System.Collections.Concurrent;

namespace DgSales.Api.Domain;

public interface ILeadRepository
{
    void Add(Lead lead);
    Lead? Get(Guid id);
    Lead? FindByPhone(string phone);
    IReadOnlyCollection<Lead> List();
}

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly ConcurrentDictionary<Guid, Lead> _leads = new();
    public void Add(Lead lead) => _leads.TryAdd(lead.Id, lead);
    public Lead? Get(Guid id) => _leads.GetValueOrDefault(id);
    public Lead? FindByPhone(string phone)
    {
        var normalized = Lead.NormalizePhone(phone);
        return _leads.Values.FirstOrDefault(x => x.Phone == normalized);
    }
    public IReadOnlyCollection<Lead> List() => _leads.Values.OrderByDescending(x => x.CreatedAtUtc).ToArray();
}

