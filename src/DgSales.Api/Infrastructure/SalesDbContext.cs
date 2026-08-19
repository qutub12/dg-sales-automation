using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<CallJob> CallJobs => Set<CallJob>();
    public DbSet<CustomerRequirement> CustomerRequirements => Set<CustomerRequirement>();
    public DbSet<Quotation> Quotations => Set<Quotation>();

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
    }
}
