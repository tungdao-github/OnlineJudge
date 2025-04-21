using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineJudgeAPI.Migrations
{
    /// <inheritdoc />
    public partial class NameMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DangBai",
                table: "Problems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DoKho",
                table: "Problems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DangBai",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "DoKho",
                table: "Problems");
        }
    }
}
