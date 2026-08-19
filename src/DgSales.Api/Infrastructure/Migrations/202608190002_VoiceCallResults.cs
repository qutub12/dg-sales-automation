using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608190002_VoiceCallResults")]
public sealed class VoiceCallResults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "voice_call_results",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                call_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                provider_call_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                detected_language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                automation_disclosed = table.Column<bool>(type: "boolean", nullable: false),
                recording_consent_given = table.Column<bool>(type: "boolean", nullable: false),
                transcript = table.Column<string>(type: "text", nullable: true),
                completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_voice_call_results", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_voice_call_results_call_job_id", table: "voice_call_results", column: "call_job_id", unique: true);
        migrationBuilder.CreateIndex(name: "ix_voice_call_results_provider_call_id", table: "voice_call_results", column: "provider_call_id", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("voice_call_results");
}
