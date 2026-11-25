using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoC_API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailMessageId",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "Campaigns");
        }
    }
}
