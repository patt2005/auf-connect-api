using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AufConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Events");
        }
    }
}
