using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<CallJob> CallJobs => Set<CallJob>();
    public DbSet<CustomerRequirement> CustomerRequirements => Set<CustomerRequirement>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<WhatsAppDeliveryJob> WhatsAppDeliveryJobs => Set<WhatsAppDeliveryJob>();
    public DbSet<VoiceCallResult> VoiceCallResults => Set<VoiceCallResult>();
    public DbSet<InboundLeadMessage> InboundLeadMessages => Set<InboundLeadMessage>();
    public DbSet<FollowUpJob> FollowUpJobs => Set<FollowUpJob>();
    public DbSet<CustomerReply> CustomerReplies => Set<CustomerReply>();
    public DbSet<OwnerNotificationJob> OwnerNotificationJobs => Set<OwnerNotificationJob>();
    public DbSet<PriceCatalogueEntry> PriceCatalogueEntries => Set<PriceCatalogueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lead = modelBuilder.Entity<Lead>();
        lead.ToTable("leads");
        lead.HasKey(x => x.Id);
        lead.Property(x => x.Id).HasColumnName("id");
        lead.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
        lead.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(16).IsRequired();
        lead.HasIndex(x => x.Phone).IsUnique();
        lead.Property(x => x.City).HasColumnName("city").HasMaxLength(120);
        lead.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(40);
        lead.Property(x => x.SourceReference).HasColumnName("source_reference").HasMaxLength(250);
        lead.Property(x => x.IsInServiceArea).HasColumnName("is_in_service_area");
        lead.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
        lead.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasConversion<string>().HasMaxLength(20);
        lead.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        lead.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        lead.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();

        var callJob = modelBuilder.Entity<CallJob>();
        callJob.ToTable("call_jobs");
        callJob.HasKey(x => x.Id);
        callJob.Property(x => x.Id).HasColumnName("id");
        callJob.Property(x => x.LeadId).HasColumnName("lead_id");
        callJob.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        callJob.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        callJob.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc");
        callJob.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        callJob.Property(x => x.ProviderCallId).HasColumnName("provider_call_id").HasMaxLength(120);
        callJob.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
        callJob.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        callJob.HasIndex(x => new { x.LeadId, x.Status })
            .HasFilter("status = 'Queued'")
            .IsUnique();

        var requirement = modelBuilder.Entity<CustomerRequirement>();
        requirement.ToTable("customer_requirements");
        requirement.HasKey(x => x.Id);
        requirement.Property(x => x.Id).HasColumnName("id");
        requirement.Property(x => x.LeadId).HasColumnName("lead_id");
        requirement.Property(x => x.RequestedKva).HasColumnName("requested_kva").HasPrecision(10, 2);
        requirement.Property(x => x.PhaseCount).HasColumnName("phase_count");
        requirement.Property(x => x.PreferredBrand).HasColumnName("preferred_brand").HasMaxLength(100);
        requirement.Property(x => x.Application).HasColumnName("application").HasMaxLength(500);
        requirement.Property(x => x.InstallationLocation).HasColumnName("installation_location").HasMaxLength(300);
        requirement.Property(x => x.SizingConfirmed).HasColumnName("sizing_confirmed");
        requirement.Property(x => x.CustomDiscountRequested).HasColumnName("custom_discount_requested");
        requirement.Property(x => x.NonStandardTermsRequested).HasColumnName("non_standard_terms_requested");
        requirement.Property(x => x.DeliveryPromiseRequired).HasColumnName("delivery_promise_required");
        requirement.Property(x => x.ValidationFlagsJson).HasColumnName("validation_flags_json").HasColumnType("jsonb");
        requirement.Property(x => x.CapturedAtUtc).HasColumnName("captured_at_utc");
        requirement.Ignore(x => x.IsComplete);
        requirement.Ignore(x => x.HasValidationFlags);
        requirement.Ignore(x => x.ValidationFlags);
        requirement.HasIndex(x => new { x.LeadId, x.CapturedAtUtc });

        var quotation = modelBuilder.Entity<Quotation>();
        quotation.ToTable("quotations");
        quotation.HasKey(x => x.Id);
        quotation.Property(x => x.Id).HasColumnName("id");
        quotation.Property(x => x.LeadId).HasColumnName("lead_id");
        quotation.Property(x => x.RequirementId).HasColumnName("requirement_id");
        quotation.HasIndex(x => x.RequirementId).IsUnique();
        quotation.Property(x => x.QuotationNumber).HasColumnName("quotation_number").HasMaxLength(40);
        quotation.HasIndex(x => x.QuotationNumber).IsUnique();
        quotation.Property(x => x.PriceVersion).HasColumnName("price_version").HasMaxLength(80);
        quotation.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100);
        quotation.Property(x => x.GensetModel).HasColumnName("genset_model").HasMaxLength(120);
        quotation.Property(x => x.Kva).HasColumnName("kva").HasPrecision(10, 2);
        quotation.Property(x => x.PhaseCount).HasColumnName("phase_count");
        quotation.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(14, 2);
        quotation.Property(x => x.GstAmount).HasColumnName("gst_amount").HasPrecision(14, 2);
        quotation.Property(x => x.GrandTotal).HasColumnName("grand_total").HasPrecision(14, 2);
        quotation.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        quotation.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

        var delivery = modelBuilder.Entity<WhatsAppDeliveryJob>();
        delivery.ToTable("whatsapp_delivery_jobs");
        delivery.HasKey(x => x.Id);
        delivery.Property(x => x.Id).HasColumnName("id");
        delivery.Property(x => x.QuotationId).HasColumnName("quotation_id");
        delivery.Property(x => x.LeadId).HasColumnName("lead_id");
        delivery.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        delivery.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        delivery.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc");
        delivery.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        delivery.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(150);
        delivery.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
        delivery.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        delivery.HasIndex(x => new { x.QuotationId, x.Status })
            .HasFilter("status = 'Queued'")
            .IsUnique();

        var callResult = modelBuilder.Entity<VoiceCallResult>();
        callResult.ToTable("voice_call_results");
        callResult.HasKey(x => x.Id);
        callResult.Property(x => x.Id).HasColumnName("id");
        callResult.Property(x => x.CallJobId).HasColumnName("call_job_id");
        callResult.HasIndex(x => x.CallJobId).IsUnique();
        callResult.Property(x => x.LeadId).HasColumnName("lead_id");
        callResult.Property(x => x.ProviderCallId).HasColumnName("provider_call_id").HasMaxLength(150);
        callResult.HasIndex(x => x.ProviderCallId).IsUnique();
        callResult.Property(x => x.Outcome).HasColumnName("outcome").HasConversion<string>().HasMaxLength(40);
        callResult.Property(x => x.DetectedLanguage).HasColumnName("detected_language").HasConversion<string>().HasMaxLength(20);
        callResult.Property(x => x.AutomationDisclosed).HasColumnName("automation_disclosed");
        callResult.Property(x => x.RecordingConsentGiven).HasColumnName("recording_consent_given");
        callResult.Property(x => x.Transcript).HasColumnName("transcript");
        callResult.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");

        var inbound = modelBuilder.Entity<InboundLeadMessage>();
        inbound.ToTable("inbound_lead_messages");
        inbound.HasKey(x => x.Id);
        inbound.Property(x => x.Id).HasColumnName("id");
        inbound.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(40);
        inbound.Property(x => x.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(300);
        inbound.HasIndex(x => new { x.Channel, x.ExternalMessageId }).IsUnique();
        inbound.Property(x => x.Sender).HasColumnName("sender").HasMaxLength(300);
        inbound.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(500);
        inbound.Property(x => x.RawText).HasColumnName("raw_text");
        inbound.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
        inbound.Property(x => x.LeadId).HasColumnName("lead_id");
        inbound.Property(x => x.ProcessingNote).HasColumnName("processing_note").HasMaxLength(1000);
        inbound.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc");
        inbound.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");

        var followUp = modelBuilder.Entity<FollowUpJob>();
        followUp.ToTable("follow_up_jobs"); followUp.HasKey(x => x.Id);
        followUp.Property(x => x.Id).HasColumnName("id"); followUp.Property(x => x.LeadId).HasColumnName("lead_id"); followUp.Property(x => x.QuotationId).HasColumnName("quotation_id");
        followUp.Property(x => x.Step).HasColumnName("step"); followUp.HasIndex(x => new { x.QuotationId, x.Step }).IsUnique();
        followUp.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(300); followUp.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        followUp.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc"); followUp.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        followUp.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(150); followUp.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
        followUp.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); followUp.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();

        var reply = modelBuilder.Entity<CustomerReply>(); reply.ToTable("customer_replies"); reply.HasKey(x => x.Id);
        reply.Property(x => x.Id).HasColumnName("id"); reply.Property(x => x.LeadId).HasColumnName("lead_id"); reply.Property(x => x.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(200); reply.HasIndex(x => x.ExternalMessageId).IsUnique();
        reply.Property(x => x.Text).HasColumnName("text").HasMaxLength(5000); reply.Property(x => x.Disposition).HasColumnName("disposition").HasConversion<string>().HasMaxLength(30); reply.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc");

        var owner = modelBuilder.Entity<OwnerNotificationJob>(); owner.ToTable("owner_notification_jobs"); owner.HasKey(x => x.Id);
        owner.Property(x => x.Id).HasColumnName("id"); owner.Property(x => x.LeadId).HasColumnName("lead_id"); owner.Property(x => x.CustomerReplyId).HasColumnName("customer_reply_id"); owner.HasIndex(x => x.CustomerReplyId).IsUnique();
        owner.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30); owner.Property(x => x.AttemptCount).HasColumnName("attempt_count"); owner.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc");
        owner.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(150); owner.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000); owner.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();

        var price = modelBuilder.Entity<PriceCatalogueEntry>(); price.ToTable("price_catalogue_entries"); price.HasKey(x => x.Id);
        price.Property(x => x.Id).HasColumnName("id"); price.Property(x => x.Version).HasColumnName("version").HasMaxLength(50); price.HasIndex(x => x.Version).IsUnique();
        price.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100); price.Property(x => x.GensetModel).HasColumnName("genset_model").HasMaxLength(120); price.Property(x => x.Kva).HasColumnName("kva").HasPrecision(10, 2); price.Property(x => x.PhaseCount).HasColumnName("phase_count");
        price.Property(x => x.BasePrice).HasColumnName("base_price").HasPrecision(14, 2); price.Property(x => x.StandardMarkup).HasColumnName("standard_markup").HasPrecision(14, 2); price.Property(x => x.TransportCharge).HasColumnName("transport_charge").HasPrecision(14, 2); price.Property(x => x.InstallationCharge).HasColumnName("installation_charge").HasPrecision(14, 2); price.Property(x => x.AccessoryCharge).HasColumnName("accessory_charge").HasPrecision(14, 2); price.Property(x => x.GstPercent).HasColumnName("gst_percent").HasPrecision(5, 2);
        price.Property(x => x.EffectiveFrom).HasColumnName("effective_from"); price.Property(x => x.EffectiveTo).HasColumnName("effective_to"); price.Property(x => x.IsActive).HasColumnName("is_active"); price.Property(x => x.ChangeReason).HasColumnName("change_reason").HasMaxLength(500); price.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        price.HasIndex(x => new { x.Brand, x.Kva, x.PhaseCount, x.IsActive });
    }
}
