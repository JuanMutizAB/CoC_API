using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoC_API.Migrations
{
    /// <inheritdoc />
    public partial class MultipleCampaignTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(
                """
                UPDATE [Campaigns]
                SET [Model] = CONCAT('[', [Model], ']')
                WHERE [Model] IS NOT NULL AND LEFT(LTRIM([Model]), 1) <> '[';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Model",
                table: "Campaigns",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
