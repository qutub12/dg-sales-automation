using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608200003_OperationsConsole")]
public sealed class OperationsConsole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("contact_allowed", "leads", "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<string>("contact_restriction_reason", "leads", "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.CreateTable("automation_controls", table => new
        {
            name = table.Column<string>("character varying(40)", maxLength: 40), is_paused = table.Column<bool>("boolean"),
            reason = table.Column<string>("character varying(500)", maxLength: 500, nullable: true), updated_at_utc = table.Column<DateTimeOffset>("timestamp with time zone")
        }, constraints: table => table.PrimaryKey("pk_automation_controls", x => x.name));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("automation_controls"); migrationBuilder.DropColumn("contact_allowed", "leads"); migrationBuilder.DropColumn("contact_restriction_reason", "leads");
    }
}
