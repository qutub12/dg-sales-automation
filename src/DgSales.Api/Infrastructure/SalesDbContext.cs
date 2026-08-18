using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<CallJob> CallJobs => Set<CallJob>();

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
    }
}
