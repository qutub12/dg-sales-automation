using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed record InboundLeadResult(InboundLeadStatus Status, Guid? LeadId, bool DuplicateMessage);

public sealed class InboundLeadProcessor(SalesDbContext db, LeadMessageParser parser, ServiceAreaMatcher serviceAreas)
{
    public async Task<InboundLeadResult> ProcessAsync(InboundLeadChannel channel, string externalId, string? sender, string? subject, string body, CancellationToken ct)
    {
        var prior = await db.InboundLeadMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Channel == channel && x.ExternalMessageId == externalId, ct);
        if (prior is not null) return new(prior.Status, prior.LeadId, true);
        var inbound = InboundLeadMessage.Receive(channel, externalId, sender, subject, body);
        db.InboundLeadMessages.Add(inbound);
        var parsed = parser.Parse(body);
        if (parsed.Phone is null)
        {
            inbound.Complete(InboundLeadStatus.ReviewRequired, null, string.Join(',', parsed.Flags));
            await db.SaveChangesAsync(ct);
            return new(inbound.Status, null, false);
        }
        var phone = Lead.NormalizePhone(parsed.Phone);
        var existing = await db.Leads.SingleOrDefaultAsync(x => x.Phone == phone, ct);
        if (existing is not null)
        {
            inbound.Complete(InboundLeadStatus.DuplicateLead, existing.Id, "Phone already exists.");
            await db.SaveChangesAsync(ct);
            return new(inbound.Status, existing.Id, false);
        }
        var source = channel == InboundLeadChannel.IndiaMartEmail ? LeadSource.IndiaMartEmail : LeadSource.JustdialWhatsApp;
        var lead = Lead.Create(new(parsed.CustomerName ?? $"{(channel == InboundLeadChannel.IndiaMartEmail ? "IndiaMART" : "Justdial")} customer", parsed.Phone, parsed.City, source, parsed.SourceReference ?? externalId), serviceAreas.IsSupported(parsed.City));
        lead.QueueCall();
        db.Leads.Add(lead);
        db.CallJobs.Add(CallJob.Queue(lead.Id));
        inbound.Complete(InboundLeadStatus.LeadCreated, lead.Id, parsed.Flags.Count == 0 ? null : string.Join(',', parsed.Flags));
        await db.SaveChangesAsync(ct);
        return new(inbound.Status, lead.Id, false);
    }
}
