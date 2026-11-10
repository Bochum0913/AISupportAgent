using AISupportAgent.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AISupportAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110000000_AddTicketOwner")]
public partial class AddTicketOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "OwnerUserId", table: "SupportTickets", type: "TEXT", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_SupportTickets_OwnerUserId", table: "SupportTickets", column: "OwnerUserId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_SupportTickets_OwnerUserId", table: "SupportTickets");
        migrationBuilder.DropColumn(name: "OwnerUserId", table: "SupportTickets");
    }
}
