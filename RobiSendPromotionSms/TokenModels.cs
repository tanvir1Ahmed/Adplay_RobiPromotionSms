using System.Text.Json.Serialization;

namespace RobiSendPromotionSms
{
    public class TokenRequestPayload
    {
        public string GrantType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;

        public FormUrlEncodedContent ToFormContent()
        {
            var formData = new Dictionary<string, string>
            {
                { "grant_type", GrantType },
                { "username", Username },
                { "password", Password },
                { "scope", Scope }
            };
            return new FormUrlEncodedContent(formData);
        }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
