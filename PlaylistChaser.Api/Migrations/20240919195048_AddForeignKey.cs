using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlaylistChaser.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSong_PlaylistId",
                table: "PlaylistSong",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSong_SongId",
                table: "PlaylistSong",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlist_UserId",
                table: "Playlist",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedPlaylistEntry_CombinedPlaylistId",
                table: "CombinedPlaylistEntry",
                column: "CombinedPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedPlaylistEntry_PlaylistId",
                table: "CombinedPlaylistEntry",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombinedPlaylistEntry_Playlist_CombinedPlaylistId",
                table: "CombinedPlaylistEntry",
                column: "CombinedPlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CombinedPlaylistEntry_Playlist_PlaylistId",
                table: "CombinedPlaylistEntry",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OAuth2Credential_User_UserId",
                table: "OAuth2Credential",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Playlist_User_UserId",
                table: "Playlist",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistInfo_Playlist_PlaylistId",
                table: "PlaylistInfo",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSong_Playlist_PlaylistId",
                table: "PlaylistSong",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSong_Song_SongId",
                table: "PlaylistSong",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SongInfo_Song_SongId",
                table: "SongInfo",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SongState_Song_SongId",
                table: "SongState",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CombinedPlaylistEntry_Playlist_CombinedPlaylistId",
                table: "CombinedPlaylistEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_CombinedPlaylistEntry_Playlist_PlaylistId",
                table: "CombinedPlaylistEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_OAuth2Credential_User_UserId",
                table: "OAuth2Credential");

            migrationBuilder.DropForeignKey(
                name: "FK_Playlist_User_UserId",
                table: "Playlist");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistInfo_Playlist_PlaylistId",
                table: "PlaylistInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSong_Playlist_PlaylistId",
                table: "PlaylistSong");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSong_Song_SongId",
                table: "PlaylistSong");

            migrationBuilder.DropForeignKey(
                name: "FK_SongInfo_Song_SongId",
                table: "SongInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_SongState_Song_SongId",
                table: "SongState");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistSong_PlaylistId",
                table: "PlaylistSong");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistSong_SongId",
                table: "PlaylistSong");

            migrationBuilder.DropIndex(
                name: "IX_Playlist_UserId",
                table: "Playlist");

            migrationBuilder.DropIndex(
                name: "IX_CombinedPlaylistEntry_CombinedPlaylistId",
                table: "CombinedPlaylistEntry");

            migrationBuilder.DropIndex(
                name: "IX_CombinedPlaylistEntry_PlaylistId",
                table: "CombinedPlaylistEntry");
        }
    }
}
