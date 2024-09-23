using Google.Apis.Http;
using System.Net.Http.Headers;

namespace PlaylistChaser.Core.Sources
{
    public class AccessTokenInitializer : IConfigurableHttpClientInitializer
    {
        private readonly string accessToken;

        public AccessTokenInitializer(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public void Initialize(ConfigurableHttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}