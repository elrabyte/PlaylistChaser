using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using static PlaylistChaser.Web.Util.BuiltInIds;

public class DatabaseDataStore : IDataStore
{
    PlaylistChaserDbContext db;
    public DatabaseDataStore(PlaylistChaserDbContext db)
    {
        this.db = db;
    }


    // Store the token data in your database
    public Task StoreAsync<T>(string key, T value)
    {
        var userId = int.Parse(key);
        var response = (TokenResponse)(object)value;

        var oAuth = db.OAuth2Credential.SingleOrDefault(c => c.UserId == userId && c.Provider == Sources.Youtube.ToString());
        if (oAuth == null)
        {
            oAuth = new OAuth2Credential
            {
                UserId = userId,
                Provider = Sources.Youtube.ToString()
            };

            db.OAuth2Credential.Add(oAuth);
        }

        oAuth.AccessToken = response.AccessToken;
        oAuth.RefreshToken = response.RefreshToken;
        oAuth.TokenExpiration = DateTime.Now.AddSeconds((double)response.ExpiresInSeconds);

        db.SaveChanges();

        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        // Remove the token data from your database
        // Example: dbContext.Tokens.Remove(key);
        var userId = int.Parse(key);

        var oAuth = db.OAuth2Credential.SingleOrDefault(c => c.UserId == userId && c.Provider == Sources.Youtube.ToString());
        db.OAuth2Credential.Remove(oAuth);
        db.SaveChanges();

        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        // Retrieve the token data from your database
        // Return the token data or null if not found
        var userId = int.Parse(key);

        var response = new TokenResponse();
        var oAuth = db.OAuth2Credential.SingleOrDefault(c => c.UserId == userId && c.Provider == Sources.Youtube.ToString());
        if (oAuth != null)
        {
            response = new TokenResponse
            {
                AccessToken = oAuth.AccessToken,
                RefreshToken = oAuth.RefreshToken
            };
        }
        return Task.FromResult<T>((T)(object)response);
    }

    public Task ClearAsync()
    {
        throw new NotImplementedException();
    }
}
