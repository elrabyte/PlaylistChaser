using System.Security.Claims;
using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using SpotifyAPI.Web;

namespace PlaylistChaser.Sources;

/// <summary>OAuth connect/callback, token refresh, and creating an <see cref="ISource"/> for a user.</summary>
public class SourceAccounts(AppDb db, IConfiguration config, IDataProtectionProvider dataProtection) : ISourceProvider
{
    private static readonly string[] SpotifyScopes = [Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate,
        Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative, Scopes.UserLibraryRead];

    public bool IsConfigured(Source source) =>
        !string.IsNullOrEmpty(ClientId(source)) && !string.IsNullOrEmpty(ClientSecret(source));

    public async Task<ISource> Get(string userId, Source source)
    {
        var account = await db.SourceAccounts.FindAsync(userId, source);
        if (account == null || account.NeedsReconnect || account.RefreshToken == "" || !IsConfigured(source))
            throw new SourceNotConnectedException(source);

        if (account.ExpiresAt < DateTime.UtcNow.AddMinutes(1))
        {
            try
            {
                var (access, refresh, expiresIn) = await Refresh(source, account.RefreshToken);
                account.AccessToken = access;
                account.RefreshToken = refresh ?? account.RefreshToken;
                account.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            }
            // only a revoked/expired grant needs a reconnect; network errors just fail this attempt
            catch (Exception ex) when (ex is TokenResponseException { Error.Error: "invalid_grant" }
                                       || ex is APIException { Response.StatusCode: System.Net.HttpStatusCode.BadRequest })
            {
                account.NeedsReconnect = true;
                await db.SaveChangesAsync();
                throw new SourceNotConnectedException(source);
            }
            await db.SaveChangesAsync();
        }

        return Create(source, account.AccessToken, account.ExternalUserId);
    }

    public static ISource Create(Source source, string accessToken, string? externalUserId) => source switch
    {
        Source.Youtube => new YoutubeSource(accessToken, externalUserId),
        Source.Spotify => new SpotifySource(accessToken, externalUserId),
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    public string LoginUrl(string userId, Source source)
    {
        var state = StateProtector().Protect($"{userId}|{source}", TimeSpan.FromMinutes(15));
        if (source == Source.Youtube)
        {
            var url = (GoogleAuthorizationCodeRequestUrl)GoogleFlow().CreateAuthorizationCodeRequest(RedirectUri(source));
            url.State = state;
            url.AccessType = "offline";
            url.Prompt = "consent"; // always return a refresh token
            return url.Build().AbsoluteUri;
        }
        return new LoginRequest(new Uri(RedirectUri(source)), ClientId(source)!, LoginRequest.ResponseType.Code)
        {
            Scope = SpotifyScopes,
            State = state,
        }.ToUri().AbsoluteUri;
    }

    /// <summary>Returns the user id the flow was started by, or null when the state is forged, foreign or expired.</summary>
    public string? ReadState(string? state, Source source)
    {
        if (string.IsNullOrEmpty(state)) return null;
        try
        {
            var parts = StateProtector().Unprotect(state).Split('|');
            return parts.Length == 2 && parts[1] == source.ToString() ? parts[0] : null;
        }
        catch (CryptographicException) { return null; }
    }

    public async Task Connect(string userId, Source source, string code)
    {
        string access, refresh, externalId;
        string? displayName;
        long expiresIn;
        if (source == Source.Youtube)
        {
            var token = await GoogleFlow().ExchangeCodeForTokenAsync(userId, code, RedirectUri(source), CancellationToken.None);
            (access, refresh, expiresIn) = (token.AccessToken, token.RefreshToken, token.ExpiresInSeconds ?? 3600);
            var yt = new YouTubeService(new BaseClientService.Initializer { HttpClientInitializer = GoogleCredential.FromAccessToken(access) });
            var request = yt.Channels.List("snippet");
            request.Mine = true;
            var channel = (await request.ExecuteAsync()).Items?.FirstOrDefault()
                          ?? throw new InvalidOperationException("This Google account has no YouTube channel.");
            (externalId, displayName) = (channel.Id, channel.Snippet.Title);
        }
        else
        {
            var token = await new OAuthClient().RequestToken(
                new SpotifyAPI.Web.AuthorizationCodeTokenRequest(ClientId(source)!, ClientSecret(source)!, code, new Uri(RedirectUri(source))));
            (access, refresh, expiresIn) = (token.AccessToken, token.RefreshToken, token.ExpiresIn);
            var me = await new SpotifyClient(access).UserProfile.Current();
            (externalId, displayName) = (me.Id, me.DisplayName);
        }

        var account = await db.SourceAccounts.FindAsync(userId, source);
        if (account == null)
            db.SourceAccounts.Add(account = new SourceAccount { UserId = userId, Source = source });
        account.AccessToken = access;
        account.RefreshToken = refresh;
        account.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        account.ExternalUserId = externalId;
        account.DisplayName = displayName;
        account.NeedsReconnect = false;
        await db.SaveChangesAsync();
    }

    private async Task<(string Access, string? Refresh, long ExpiresIn)> Refresh(Source source, string refreshToken)
    {
        if (source == Source.Youtube)
        {
            var t = await GoogleFlow().RefreshTokenAsync("user", refreshToken, CancellationToken.None);
            return (t.AccessToken, t.RefreshToken, t.ExpiresInSeconds ?? 3600);
        }
        var s = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(ClientId(source)!, ClientSecret(source)!, refreshToken));
        return (s.AccessToken, s.RefreshToken, s.ExpiresIn);
    }

    private GoogleAuthorizationCodeFlow GoogleFlow() => new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets { ClientId = ClientId(Source.Youtube), ClientSecret = ClientSecret(Source.Youtube) },
        Scopes = [YouTubeService.Scope.Youtube],
    });

    private ITimeLimitedDataProtector StateProtector() => dataProtection.CreateProtector("OAuthState").ToTimeLimitedDataProtector();
    private string? ClientId(Source s) => config[$"{s.ToString().ToUpperInvariant()}_CLIENT_ID"];
    private string? ClientSecret(Source s) => config[$"{s.ToString().ToUpperInvariant()}_CLIENT_SECRET"];
    private string RedirectUri(Source s) => $"{config["PUBLIC_URL"]!.TrimEnd('/')}/api/sources/{s.ToString().ToLowerInvariant()}/callback";
}

public static class SourceEndpoints
{
    public static void MapSourceEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/sources", async (ClaimsPrincipal user, AppDb db, SourceAccounts accounts) =>
        {
            var mine = await db.SourceAccounts.Where(a => a.UserId == user.Id()).ToListAsync();
            return Enum.GetValues<Source>().Select(s =>
            {
                var a = mine.FirstOrDefault(x => x.Source == s);
                return new { source = s, configured = accounts.IsConfigured(s), connected = a != null, needsReconnect = a != null && (a.NeedsReconnect || a.RefreshToken == ""), displayName = a?.DisplayName };
            });
        });

        api.MapGet("/sources/{source}/connect", (string source, ClaimsPrincipal user, SourceAccounts accounts) =>
            !ApiHelpers.TryParseSource(source, out var s) ? Results.NotFound()
            : !accounts.IsConfigured(s) ? Results.BadRequest($"{s} is not configured on this server.")
            : Results.Redirect(accounts.LoginUrl(user.Id(), s)));

        // Reached by a cross-site redirect from the provider, so the SameSite=Strict login cookie is not sent;
        // the protected state carries who started the flow instead.
        api.MapGet("/sources/{source}/callback", async (string source, string? code, string? state, string? error,
            SourceAccounts accounts, ILogger<SourceAccounts> log) =>
        {
            if (!ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
            var userId = accounts.ReadState(state, s);
            if (userId == null) return Results.Redirect("/settings?error=" + Uri.EscapeDataString("Connection link expired or invalid. Try again."));
            if (error != null || string.IsNullOrEmpty(code)) return Results.Redirect("/settings?error=" + Uri.EscapeDataString($"{s}: {error ?? "no code"}"));
            try { await accounts.Connect(userId, s, code); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Connecting {Source} failed", s);
                return Results.Redirect("/settings?error=" + Uri.EscapeDataString($"Connecting {s} failed: {ex.Message}"));
            }
            return Results.Redirect("/settings?connected=" + s.ToString().ToLowerInvariant());
        }).AllowAnonymous();

        // for the "add from my account" picker: includes private playlists and liked songs, which have no shareable link
        api.MapGet("/sources/{source}/playlists", (string source, ClaimsPrincipal user, ISourceProvider sources, ILogger<SourceAccounts> log) =>
            Playlists.PlaylistEndpoints.Guard(log, async () =>
            {
                if (!ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
                var mine = await (await sources.Get(user.Id(), s)).GetMyPlaylists();
                return Results.Ok(mine.Select(p => new { p.Id, p.Name, p.Creator, p.ThumbnailUrl, p.IsMine, Url = SourceUrls.Playlist(s, p.Id) }));
            }));

        api.MapDelete("/sources/{source}", async (string source, ClaimsPrincipal user, AppDb db) =>
        {
            if (!ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
            await db.SourceAccounts.Where(a => a.UserId == user.Id() && a.Source == s).ExecuteDeleteAsync();
            return Results.NoContent();
        });
    }
}
