using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISupportAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "SupportTickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "SupportTickets");
        }
    }
}
