using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AufConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteProjectLinksToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FavoriteProjectLinks",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteProjectLinks",
                table: "Users");
        }
    }
}
