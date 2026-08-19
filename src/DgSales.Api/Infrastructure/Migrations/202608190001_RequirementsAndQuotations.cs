using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608190001_RequirementsAndQuotations")]
public sealed class RequirementsAndQuotations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "customer_requirements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                requested_kva = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                phase_count = table.Column<int>(type: "integer", nullable: false),
                preferred_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                application = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                installation_location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                sizing_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                custom_discount_requested = table.Column<bool>(type: "boolean", nullable: false),
                non_standard_terms_requested = table.Column<bool>(type: "boolean", nullable: false),
                delivery_promise_required = table.Column<bool>(type: "boolean", nullable: false),
                validation_flags_json = table.Column<string>(type: "jsonb", nullable: false),
                captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_customer_requirements", x => x.id));
        migrationBuilder.CreateIndex(
            name: "ix_customer_requirements_lead_id_captured_at_utc",
            table: "customer_requirements",
            columns: ["lead_id", "captured_at_utc"]);

        migrationBuilder.CreateTable(
            name: "quotations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                quotation_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                price_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                genset_model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                kva = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                phase_count = table.Column<int>(type: "integer", nullable: false),
                subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                gst_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                grand_total = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_quotations", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_quotations_quotation_number", table: "quotations", column: "quotation_number", unique: true);
        migrationBuilder.CreateIndex(name: "ix_quotations_requirement_id", table: "quotations", column: "requirement_id", unique: true);

        migrationBuilder.CreateTable(
            name: "whatsapp_delivery_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                provider_message_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_whatsapp_delivery_jobs", x => x.id));
        migrationBuilder.CreateIndex(
            name: "ix_whatsapp_delivery_jobs_quotation_id_status",
            table: "whatsapp_delivery_jobs",
            columns: ["quotation_id", "status"],
            unique: true,
            filter: "status = 'Queued'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("whatsapp_delivery_jobs");
        migrationBuilder.DropTable("quotations");
        migrationBuilder.DropTable("customer_requirements");
    }
}
