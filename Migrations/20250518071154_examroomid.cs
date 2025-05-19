using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineJudgeAPI.Migrations
{
    /// <inheritdoc />
    public partial class examroomid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExamRoomId",
                table: "Submissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExamRoomId",
                table: "Submissions",
                column: "ExamRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_ExamRooms_ExamRoomId",
                table: "Submissions",
                column: "ExamRoomId",
                principalTable: "ExamRooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_ExamRooms_ExamRoomId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ExamRoomId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ExamRoomId",
                table: "Submissions");
        }
    }
}
