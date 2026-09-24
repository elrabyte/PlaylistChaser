namespace PlaylistChaser.Web.Models.Api
{
    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string UserName, int UserId);
    public record MergeSongsRequest(List<int> SongIds, int? MainSongId);
}
