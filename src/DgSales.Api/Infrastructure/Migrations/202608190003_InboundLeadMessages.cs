using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608190003_InboundLeadMessages")]
public sealed class InboundLeadMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inbound_lead_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                external_message_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                sender = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                raw_text = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                processing_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            }, constraints: table => table.PrimaryKey("PK_inbound_lead_messages", x => x.id));
        migrationBuilder.CreateIndex(name: "IX_inbound_lead_messages_channel_external_message_id", table: "inbound_lead_messages", columns: new[] { "channel", "external_message_id" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "inbound_lead_messages");
}
