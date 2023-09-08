using Google.Apis.Http;

public class AccessTokenInitializer : IConfigurableHttpClientInitializer
{
    private readonly string accessToken;

    public AccessTokenInitializer(string accessToken)
    {
        this.accessToken = accessToken;
    }

    public void Initialize(ConfigurableHttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}
