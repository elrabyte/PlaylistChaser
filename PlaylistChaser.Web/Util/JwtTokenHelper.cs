using Microsoft.IdentityModel.Tokens;
using PlaylistChaser.Web.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlaylistChaser.Web.Util
{
    /// <summary>
    /// Issues/validates the JWT bearer tokens used by the REST API (see Controllers/Api).
    /// The existing MVC/Razor UI keeps using cookie auth; the API uses this token scheme so
    /// any client (the React SPA, a mobile app, ...) can authenticate without cookies.
    /// Configuration lives under the "Jwt" section in appsettings (Key/Issuer/Audience/ExpiryMinutes).
    /// </summary>
    public class JwtTokenHelper
    {
        private readonly IConfiguration configuration;

        public JwtTokenHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string CreateToken(User user, IEnumerable<string>? roles = null)
        {
            var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = configuration["Jwt:Issuer"] ?? "PlaylistChaser";
            var audience = configuration["Jwt:Audience"] ?? "PlaylistChaser";
            var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
            };
            if (roles != null)
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
