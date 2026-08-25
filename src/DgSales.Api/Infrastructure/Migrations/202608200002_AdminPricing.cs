using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608200002_AdminPricing")]
public sealed class AdminPricing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("price_catalogue_entries", table => new
        {
            id = table.Column<Guid>("uuid"), version = table.Column<string>("character varying(50)", maxLength: 50), brand = table.Column<string>("character varying(100)", maxLength: 100),
            genset_model = table.Column<string>("character varying(120)", maxLength: 120), kva = table.Column<decimal>("numeric(10,2)"), phase_count = table.Column<int>("integer"),
            base_price = table.Column<decimal>("numeric(14,2)"), standard_markup = table.Column<decimal>("numeric(14,2)"), transport_charge = table.Column<decimal>("numeric(14,2)"),
            installation_charge = table.Column<decimal>("numeric(14,2)"), accessory_charge = table.Column<decimal>("numeric(14,2)"), gst_percent = table.Column<decimal>("numeric(5,2)"),
            effective_from = table.Column<DateOnly>("date"), effective_to = table.Column<DateOnly>("date", nullable: true), is_active = table.Column<bool>("boolean"),
            change_reason = table.Column<string>("character varying(500)", maxLength: 500), created_at_utc = table.Column<DateTimeOffset>("timestamp with time zone")
        }, constraints: table => table.PrimaryKey("pk_price_catalogue_entries", x => x.id));
        migrationBuilder.CreateIndex("ix_price_catalogue_entries_version", "price_catalogue_entries", "version", unique: true);
        migrationBuilder.CreateIndex("ix_price_catalogue_entries_brand_kva_phase_count_is_active", "price_catalogue_entries", new[] { "brand", "kva", "phase_count", "is_active" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("price_catalogue_entries");
}
