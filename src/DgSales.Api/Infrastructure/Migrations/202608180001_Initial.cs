using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608180001_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "leads",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                source_reference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                preferred_language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_leads", x => x.id));

        migrationBuilder.CreateIndex(name: "ix_leads_phone", table: "leads", column: "phone", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("leads");
}
