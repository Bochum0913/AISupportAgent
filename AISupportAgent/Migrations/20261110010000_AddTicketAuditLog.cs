using AISupportAgent.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AISupportAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110010000_AddTicketAuditLog")]
public partial class AddTicketAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketAuditEntries",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TicketId = table.Column<string>(type: "TEXT", nullable: false),
                FieldName = table.Column<string>(type: "TEXT", nullable: false),
                OldValue = table.Column<string>(type: "TEXT", nullable: false),
                NewValue = table.Column<string>(type: "TEXT", nullable: false),
                ActorUserId = table.Column<string>(type: "TEXT", nullable: false),
                ActorLabel = table.Column<string>(type: "TEXT", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_TicketAuditEntries", x => x.Id); });

        migrationBuilder.CreateIndex(
            name: "IX_TicketAuditEntries_TicketId_OccurredAtUtc",
            table: "TicketAuditEntries",
            columns: new[] { "TicketId", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TicketAuditEntries");
    }
}
