using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenKNX.IoT.Migrations
{
    /// <inheritdoc />
    public partial class ResourceEraseCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EraseCodes",
                table: "Resources",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EraseCodes",
                table: "Resources");
        }
    }
}
