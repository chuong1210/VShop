using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api_be.DB.Migrations
{
    /// <inheritdoc />
    public partial class FixUserVerificationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserVerifications_Users_UserId1",
                table: "UserVerifications");

            migrationBuilder.DropIndex(
                name: "IX_UserVerifications_UserId1",
                table: "UserVerifications");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserVerifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "UserVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_UserId1",
                table: "UserVerifications",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVerifications_Users_UserId1",
                table: "UserVerifications",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
