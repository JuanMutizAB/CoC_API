using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoC_API.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookPostId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookPostId",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookPostId",
                table: "Campaigns");
        }
    }
}
