using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoC_API.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignEmailRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailRecipients",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailRecipients",
                table: "Campaigns");
        }
    }
}
