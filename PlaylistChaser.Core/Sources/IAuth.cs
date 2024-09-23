
using PlaylistChaser.Model;

namespace PlaylistChaser.Core.Sources
{
    public interface IAuth : ISourceBase
    {
        public Uri GetLoginUri(string clientId, string redirectUri);
        Task<OAuth2Credential> GetOAuthCredential(string clientId, string clientSecret, string refreshToken, int userId);
        Task<OAuth2Credential> GetOAuthCredential(string code, string clientId, string clientSecret, string redirectUri, int userId);
    }
}