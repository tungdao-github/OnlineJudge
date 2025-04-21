using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineJudgeAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitWebDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestStandings_Contests_ContestId",
                table: "ContestStandings");

            migrationBuilder.AddColumn<string>(
                name: "Constraints",
                table: "Problems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "ContestStandings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ContestId",
                table: "ContestStandings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_ContestStandings_Contests_ContestId",
                table: "ContestStandings",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestStandings_Contests_ContestId",
                table: "ContestStandings");

            migrationBuilder.DropColumn(
                name: "Constraints",
                table: "Problems");

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "ContestStandings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ContestId",
                table: "ContestStandings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContestStandings_Contests_ContestId",
                table: "ContestStandings",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
