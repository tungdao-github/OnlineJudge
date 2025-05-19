using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineJudgeAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateExamStudentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamStudents",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),

                    ExamRoomId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    ExamPaperId = table.Column<int>(nullable: false),

                    FullName = table.Column<string>(nullable: true),
                    IdentityCard = table.Column<string>(nullable: true),
                    SeatCode = table.Column<string>(nullable: true),
                    ExamCode = table.Column<string>(nullable: true),
                    FeeStatus = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamStudents", x => x.Id);

                    table.ForeignKey(
                        name: "FK_ExamStudents_ExamRooms_ExamRoomId",
                        column: x => x.ExamRoomId,
                        principalTable: "ExamRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_ExamStudents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_ExamStudents_ExamPapers_ExamPaperId",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamStudents_ExamRoomId",
                table: "ExamStudents",
                column: "ExamRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStudents_UserId",
                table: "ExamStudents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStudents_ExamPaperId",
                table: "ExamStudents",
                column: "ExamPaperId");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExamStudents");
        }
    }
}
