using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlaylistChaser.Api.Migrations
{
    /// <inheritdoc />
    public partial class renamedfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainSourceId",
                table: "Playlist");

            migrationBuilder.AddColumn<int>(
                name: "OriginSourceId",
                table: "Playlist",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginSourceId",
                table: "Playlist");

            migrationBuilder.AddColumn<int>(
                name: "MainSourceId",
                table: "Playlist",
                type: "integer",
                nullable: true);
        }
    }
}
