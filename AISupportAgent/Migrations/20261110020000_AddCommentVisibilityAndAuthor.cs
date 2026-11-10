using AISupportAgent.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AISupportAgent.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110020000_AddCommentVisibilityAndAuthor")]
public partial class AddCommentVisibilityAndAuthor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "AuthorUserId", table: "SupportTicketComments",
            type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "AuthorLabel", table: "SupportTicketComments",
            type: "TEXT", nullable: false, defaultValue: "Legacy");
        migrationBuilder.AddColumn<bool>(name: "IsInternal", table: "SupportTicketComments",
            type: "INTEGER", nullable: false, defaultValue: false);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AuthorUserId", table: "SupportTicketComments");
        migrationBuilder.DropColumn(name: "AuthorLabel", table: "SupportTicketComments");
        migrationBuilder.DropColumn(name: "IsInternal", table: "SupportTicketComments");
    }
}
