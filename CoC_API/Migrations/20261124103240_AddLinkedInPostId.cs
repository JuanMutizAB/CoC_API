using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoC_API.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedInPostId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedInPostId",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedInPostId",
                table: "Campaigns");
        }
    }
}
