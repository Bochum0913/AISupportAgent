using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISupportAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCommentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketComments_TicketId",
                table: "SupportTicketComments",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportTicketComments_SupportTickets_TicketId",
                table: "SupportTicketComments",
                column: "TicketId",
                principalTable: "SupportTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportTicketComments_SupportTickets_TicketId",
                table: "SupportTicketComments");

            migrationBuilder.DropIndex(
                name: "IX_SupportTicketComments_TicketId",
                table: "SupportTicketComments");
        }
    }
}
