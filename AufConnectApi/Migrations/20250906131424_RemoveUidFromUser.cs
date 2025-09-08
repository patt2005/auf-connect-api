using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AufConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUidFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Uid",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
