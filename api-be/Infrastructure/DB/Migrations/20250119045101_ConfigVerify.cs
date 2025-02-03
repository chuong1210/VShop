using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api_be.DB.Migrations
{
    /// <inheritdoc />
    public partial class ConfigVerify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EmailVerifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "OTPCode",
                table: "EmailVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "EmailVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_UserId1",
                table: "EmailVerifications",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerifications_Users_UserId1",
                table: "EmailVerifications",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerifications_Users_UserId1",
                table: "EmailVerifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerifications_UserId1",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "OTPCode",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "EmailVerifications");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EmailVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
