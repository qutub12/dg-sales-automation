using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608250001_OwnerWhatsAppQuotationApproval")]
public sealed class OwnerWhatsAppQuotationApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>("selling_price", "quotations", "numeric(14,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("transport_charge", "quotations", "numeric(14,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("installation_charge", "quotations", "numeric(14,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<bool>("installation_required", "customer_requirements", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.CreateTable("owner_quotation_approvals", table => new
        {
            id = table.Column<Guid>("uuid"), lead_id = table.Column<Guid>("uuid"), requirement_id = table.Column<Guid>("uuid"), quotation_id = table.Column<Guid>("uuid", nullable: true),
            request_code = table.Column<string>("character varying(20)", maxLength: 20), brand = table.Column<string>("character varying(100)", maxLength: 100), genset_model = table.Column<string>("character varying(120)", maxLength: 120),
            kva = table.Column<decimal>("numeric(10,2)"), phase_count = table.Column<int>("integer"), installation_required = table.Column<bool>("boolean"), selling_price = table.Column<decimal>("numeric(14,2)", nullable: true), transport_charge = table.Column<decimal>("numeric(14,2)", nullable: true), installation_charge = table.Column<decimal>("numeric(14,2)", nullable: true), gst_percent = table.Column<decimal>("numeric(5,2)"),
            status = table.Column<string>("character varying(40)", maxLength: 40), provider_message_id = table.Column<string>("character varying(150)", maxLength: 150, nullable: true), owner_reply = table.Column<string>("character varying(2000)", maxLength: 2000, nullable: true), last_error = table.Column<string>("character varying(1000)", maxLength: 1000, nullable: true), attempt_count = table.Column<int>("integer"), scheduled_at_utc = table.Column<DateTimeOffset>("timestamp with time zone"), created_at_utc = table.Column<DateTimeOffset>("timestamp with time zone"), updated_at_utc = table.Column<DateTimeOffset>("timestamp with time zone")
        }, constraints: table => table.PrimaryKey("pk_owner_quotation_approvals", x => x.id));
        migrationBuilder.CreateIndex("ix_owner_quotation_approvals_requirement_id", "owner_quotation_approvals", "requirement_id", unique: true);
        migrationBuilder.CreateIndex("ix_owner_quotation_approvals_quotation_id", "owner_quotation_approvals", "quotation_id", unique: true);
        migrationBuilder.CreateIndex("ix_owner_quotation_approvals_request_code", "owner_quotation_approvals", "request_code", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("owner_quotation_approvals");
        migrationBuilder.DropColumn("selling_price", "quotations");
        migrationBuilder.DropColumn("transport_charge", "quotations");
        migrationBuilder.DropColumn("installation_charge", "quotations");
        migrationBuilder.DropColumn("installation_required", "customer_requirements");
    }
}
