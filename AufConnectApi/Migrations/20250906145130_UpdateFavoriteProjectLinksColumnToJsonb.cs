using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AufConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFavoriteProjectLinksColumnToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Update existing empty string values to empty JSON array
            migrationBuilder.Sql("UPDATE \"Users\" SET \"FavoriteProjectLinks\" = '[]' WHERE \"FavoriteProjectLinks\" = '';");
            
            // Step 2: Drop the default constraint first
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"FavoriteProjectLinks\" DROP DEFAULT;");
            
            // Step 3: Convert the column type to jsonb with proper USING clause
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"FavoriteProjectLinks\" TYPE jsonb USING CASE WHEN \"FavoriteProjectLinks\" = '' THEN '[]'::jsonb ELSE \"FavoriteProjectLinks\"::jsonb END;");
            
            // Step 4: Set new default as JSON array
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"FavoriteProjectLinks\" SET DEFAULT '[]'::jsonb;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FavoriteProjectLinks",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
