using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenKNX.IoT.Migrations
{
    /// <inheritdoc />
    public partial class ResourceDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Default",
                table: "Resources",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Default",
                table: "Resources");
        }
    }
}
