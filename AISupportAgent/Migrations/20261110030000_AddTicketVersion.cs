using AISupportAgent.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AISupportAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110030000_AddTicketVersion")]
public partial class AddTicketVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(name: "Version", table: "SupportTickets",
            type: "INTEGER", nullable: false, defaultValue: 1L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Version", table: "SupportTickets");
    }
}
