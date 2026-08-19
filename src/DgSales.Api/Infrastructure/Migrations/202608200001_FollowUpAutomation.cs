using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgSales.Api.Infrastructure.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("202608200001_FollowUpAutomation")]
public sealed class FollowUpAutomation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("follow_up_jobs", table => new
        {
            id = table.Column<Guid>("uuid"), lead_id = table.Column<Guid>("uuid"), quotation_id = table.Column<Guid>("uuid"), step = table.Column<int>("integer"),
            purpose = table.Column<string>("character varying(300)", maxLength: 300), status = table.Column<string>("character varying(30)", maxLength: 30),
            scheduled_at_utc = table.Column<DateTimeOffset>("timestamp with time zone"), attempt_count = table.Column<int>("integer"),
            provider_message_id = table.Column<string>("character varying(150)", maxLength: 150, nullable: true), last_error = table.Column<string>("character varying(1000)", maxLength: 1000, nullable: true),
            created_at_utc = table.Column<DateTimeOffset>("timestamp with time zone"), xmin = table.Column<uint>("xid", rowVersion: true)
        }, constraints: table => table.PrimaryKey("pk_follow_up_jobs", x => x.id));
        migrationBuilder.CreateIndex("ix_follow_up_jobs_quotation_id_step", "follow_up_jobs", new[] { "quotation_id", "step" }, unique: true);

        migrationBuilder.CreateTable("customer_replies", table => new
        {
            id = table.Column<Guid>("uuid"), lead_id = table.Column<Guid>("uuid"), external_message_id = table.Column<string>("character varying(200)", maxLength: 200),
            text = table.Column<string>("character varying(5000)", maxLength: 5000), disposition = table.Column<string>("character varying(30)", maxLength: 30), received_at_utc = table.Column<DateTimeOffset>("timestamp with time zone")
        }, constraints: table => table.PrimaryKey("pk_customer_replies", x => x.id));
        migrationBuilder.CreateIndex("ix_customer_replies_external_message_id", "customer_replies", "external_message_id", unique: true);

        migrationBuilder.CreateTable("owner_notification_jobs", table => new
        {
            id = table.Column<Guid>("uuid"), lead_id = table.Column<Guid>("uuid"), customer_reply_id = table.Column<Guid>("uuid"),
            status = table.Column<string>("character varying(30)", maxLength: 30), attempt_count = table.Column<int>("integer"), scheduled_at_utc = table.Column<DateTimeOffset>("timestamp with time zone"),
            provider_message_id = table.Column<string>("character varying(150)", maxLength: 150, nullable: true), last_error = table.Column<string>("character varying(1000)", maxLength: 1000, nullable: true), xmin = table.Column<uint>("xid", rowVersion: true)
        }, constraints: table => table.PrimaryKey("pk_owner_notification_jobs", x => x.id));
        migrationBuilder.CreateIndex("ix_owner_notification_jobs_customer_reply_id", "owner_notification_jobs", "customer_reply_id", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("owner_notification_jobs"); migrationBuilder.DropTable("customer_replies"); migrationBuilder.DropTable("follow_up_jobs");
    }
}
