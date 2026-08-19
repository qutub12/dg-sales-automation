using DgSales.Api.Domain;
using DgSales.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Application;

public sealed class CustomerReplyService(SalesDbContext db)
{
    public async Task<bool> ProcessAsync(string externalId, string phone, string text, CancellationToken ct)
    {
        if (await db.CustomerReplies.AnyAsync(x => x.ExternalMessageId == externalId, ct)) return true;
        var normalized = Lead.NormalizePhone(phone);
        var lead = await db.Leads.SingleOrDefaultAsync(x => x.Phone == normalized, ct);
        if (lead is null) return false;
        var disposition = Classify(text);
        var reply = CustomerReply.Capture(lead.Id, externalId, text, disposition);
        db.CustomerReplies.Add(reply);
        var pending = await db.FollowUpJobs.Where(x => x.LeadId == lead.Id && x.Status == FollowUpStatus.Queued).ToListAsync(ct);
        pending.ForEach(x => x.Cancel());
        if (disposition == CustomerReplyDisposition.Accepted) lead.MarkWon();
        else if (disposition == CustomerReplyDisposition.Rejected) lead.MarkLost();
        else lead.MarkEscalated();
        if (disposition is not CustomerReplyDisposition.Rejected)
            db.OwnerNotificationJobs.Add(OwnerNotificationJob.Queue(lead.Id, reply.Id));
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static CustomerReplyDisposition Classify(string text)
    {
        var value = text.Trim().ToLowerInvariant();
        if (Contains(value, "accept", "confirm", "book", "deal", "हो", "मंजूर", "मान्य")) return CustomerReplyDisposition.Accepted;
        if (Contains(value, "not interested", "reject", "cancel", "नको", "नहीं चाहिए", "नाही पाहिजे")) return CustomerReplyDisposition.Rejected;
        if (Contains(value, "call me", "human", "owner", "भाई", "बोलना", "फोन करा", "कॉल करा")) return CustomerReplyDisposition.HumanHelp;
        if (Contains(value, "interested", "price", "discount", "visit", "details", "quotation", "कोटेशन", "किंमत")) return CustomerReplyDisposition.Interested;
        return CustomerReplyDisposition.Other;
    }

    private static bool Contains(string value, params string[] terms) => terms.Any(value.Contains);
}
