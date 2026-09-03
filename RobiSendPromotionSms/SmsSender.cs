using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RobiSendPromotionSms
{
    /// <summary>
    /// Sends a promotional SMS through the Robi APIGate.
    /// Mirrors RobiDobService.SendSms from the robi_gamestar_api project.
    /// </summary>
    public class SmsSender
    {
        private readonly HttpClient _httpClient;

        // Credentials and endpoints, as used by the GameStar implementation.
        private const string AuthorizationToken =
            "Basic YndmZjJsaHFMdllVdU1iaHVOMmFaYkdYSXJNYTpNSWNqQzhpUTdSSkZaNFRyTlJsZkxOX3RjV2dh";
        private const string TokenUrl = "https://apigate.robi.com.bd/token";
        private const string Username = "Mife_VUMobile";
        private const string Password = "VEuM0B1le#tvfi3a";
        private const string Scope = "PRODUCTION";

        // The short code used when no sender is given.
        public const string DefaultSenderAddress = "25063";

        // The outbound URL always uses the 25063 short code, exactly as the working
        // /sms/send-sms API does. The gateway takes the actual sender from the
        // request body, so an alphanumeric sender ID goes there, not in the path.
        private const string SmsUrl =
            "https://apigate.robi.com.bd/Ext/smsmessaging/v1/outbound/tel:25063/requests";

        private const string ClientCorrelator = "Adplay Technology";
        private const string NotifyUrl = "http://127.0.0.1/rest_test.php";
        private const string CallbackData = "some-data-useful-to-the-requester";
        private const string SenderName = "Gamestar";

        // UnsafeRelaxedJsonEscaping keeps Bangla message text intact instead of
        // emitting \uXXXX escapes.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Cached bearer token. Tokens last an hour, so a bulk run reuses one
        // instead of minting a fresh token per message.
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

        public SmsSender(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Returns a valid bearer token, reusing the cached one until it is close
        /// to expiring.
        /// </summary>
        private async Task<string?> GetAccessToken()
        {
            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                return _cachedToken;
            }

            var (token, expiresIn) = await GenerateTokenWithLifetime();
            _cachedToken = token;

            // Renew a minute early so a token cannot expire mid-request.
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(expiresIn - 60, 0));
            return _cachedToken;
        }

        /// <summary>
        /// Requests an OAuth bearer token from the Robi APIGate.
        /// </summary>
        public async Task<string?> GenerateToken()
        {
            var (token, _) = await GenerateTokenWithLifetime();
            return token;
        }

        private async Task<(string? Token, int ExpiresIn)> GenerateTokenWithLifetime()
        {
            var payload = new TokenRequestPayload
            {
                GrantType = "password",
                Username = Username,
                Password = Password,
                Scope = Scope
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = payload.ToFormContent()
            };

            var parts = AuthorizationToken.Split(' ', 2);
            request.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<TokenErrorResponse>(responseContent);
                throw new Exception($"Error: {errorResponse?.Error} - {errorResponse?.ErrorDescription}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
            return (tokenResponse?.AccessToken, tokenResponse?.ExpiresIn ?? 0);
        }

        /// <summary>
        /// Sends an SMS to a single number. Accepts the number with or without the
        /// 88 country prefix. Returns the raw API response on success.
        /// </summary>
        /// <param name="msisdn">Recipient number.</param>
        /// <param name="message">Message text.</param>
        /// <param name="senderAddress">
        /// Sender the message appears from: either the 25063 short code or an
        /// approved alphanumeric sender ID such as DARUN_OFFER.
        /// </param>
        public async Task<string> SendSms(
            string msisdn,
            string message,
            string senderAddress = DefaultSenderAddress)
        {
            if (msisdn.StartsWith("88"))
            {
                msisdn = msisdn.Substring(2); // "01852956967"
            }

            var accessToken = await GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("Failed to generate access token.");
            var body = new
            {
                outboundSMSMessageRequest = new
                {
                    address = new[] { "tel:+88" + msisdn },
                    senderAddress = "tel:" + senderAddress,
                    outboundSMSTextMessage = new
                    {
                        message = message
                    },
                    clientCorrelator = ClientCorrelator,
                    receiptRequest = new
                    {
                        notifyURL = NotifyUrl,
                        callbackData = CallbackData,
                        senderName = SenderName
                    }
                }
            };

            var jsonBody = JsonSerializer.Serialize(body, SerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, SmsUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            // Headers are set per-request rather than on DefaultRequestHeaders so a
            // shared HttpClient is never mutated across calls.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SMS API Error: {response.StatusCode} - {responseContent}");
            }

            return responseContent;
        }
    }
}
