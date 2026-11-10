using AISupportAgent.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AISupportAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110040000_AddTicketNumberSequence")]
public partial class AddTicketNumberSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketNumberSequences",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                NextNumber = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_TicketNumberSequences", x => x.Id));

        // Existing IDs, comments, and audit entries are not touched. The counter
        // begins beyond the highest numeric INC-* ticket, not the newest CreatedAt.
        migrationBuilder.Sql(@"
            INSERT INTO ""TicketNumberSequences"" (""Id"", ""NextNumber"")
            SELECT 1, MAX(1001, COALESCE(MAX(CAST(SUBSTR(""Id"", 5) AS INTEGER)), 1000) + 1)
            FROM ""SupportTickets""
            WHERE ""Id"" GLOB 'INC-[0-9]*'
              AND SUBSTR(""Id"", 5) NOT GLOB '*[^0-9]*';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TicketNumberSequences");
    }
}
