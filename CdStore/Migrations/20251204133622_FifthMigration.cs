using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdStore.Migrations
{
    /// <inheritdoc />
    public partial class FifthMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Opis",
                table: "Kategorie");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Opis",
                table: "Kategorie",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
