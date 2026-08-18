using DgSales.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DgSales.Api.Infrastructure;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();

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
        lead.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
        lead.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasConversion<string>().HasMaxLength(20);
        lead.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        lead.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        lead.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
    }
}
