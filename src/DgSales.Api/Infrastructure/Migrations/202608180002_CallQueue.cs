using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608180002_CallQueue")]
public sealed class CallQueue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "is_in_service_area", table: "leads", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.CreateTable(
            name: "call_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                provider_call_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_call_jobs", x => x.id));
        migrationBuilder.CreateIndex(
            name: "ix_call_jobs_lead_id_status",
            table: "call_jobs",
            columns: ["lead_id", "status"],
            unique: true,
            filter: "status = 'Queued'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("call_jobs");
        migrationBuilder.DropColumn(name: "is_in_service_area", table: "leads");
    }
}
