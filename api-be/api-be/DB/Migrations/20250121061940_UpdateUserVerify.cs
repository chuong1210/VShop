using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api_be.DB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserVerify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerifications_Users_UserId",
                table: "EmailVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerifications_Users_UserId1",
                table: "EmailVerifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailVerifications",
                table: "EmailVerifications");

            migrationBuilder.RenameTable(
                name: "EmailVerifications",
                newName: "UserVerifications");

            migrationBuilder.RenameIndex(
                name: "IX_EmailVerifications_UserId1",
                table: "UserVerifications",
                newName: "IX_UserVerifications_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_EmailVerifications_UserId",
                table: "UserVerifications",
                newName: "IX_UserVerifications_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserVerifications",
                table: "UserVerifications",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVerifications_Users_UserId",
                table: "UserVerifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserVerifications_Users_UserId1",
                table: "UserVerifications",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserVerifications_Users_UserId",
                table: "UserVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserVerifications_Users_UserId1",
                table: "UserVerifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserVerifications",
                table: "UserVerifications");

            migrationBuilder.RenameTable(
                name: "UserVerifications",
                newName: "EmailVerifications");

            migrationBuilder.RenameIndex(
                name: "IX_UserVerifications_UserId1",
                table: "EmailVerifications",
                newName: "IX_EmailVerifications_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserVerifications_UserId",
                table: "EmailVerifications",
                newName: "IX_EmailVerifications_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailVerifications",
                table: "EmailVerifications",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerifications_Users_UserId",
                table: "EmailVerifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerifications_Users_UserId1",
                table: "EmailVerifications",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
