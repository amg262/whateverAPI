using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace whateverAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookProviderid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookId",
                table: "Users");
        }
    }
}
