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

        // The short code the message is sent from. It appears both in the request
        // body as senderAddress and in the outbound URL path.
        private const string SenderAddress = "25063";
        private const string SmsUrl =
            "https://apigate.robi.com.bd/Ext/smsmessaging/v1/outbound/tel:" + SenderAddress + "/requests";

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

        public SmsSender(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Requests an OAuth bearer token from the Robi APIGate.
        /// </summary>
        public async Task<string?> GenerateToken()
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
            return tokenResponse?.AccessToken;
        }

        /// <summary>
        /// Sends an SMS to a single number. Accepts the number with or without the
        /// 88 country prefix. Returns the raw API response on success.
        /// </summary>
        public async Task<string> SendSms(string msisdn, string message)
        {
            if (msisdn.StartsWith("88"))
            {
                msisdn = msisdn.Substring(2); // "01852956967"
            }

            var accessToken = await GenerateToken();

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("Failed to generate access token.");

            var body = new
            {
                outboundSMSMessageRequest = new
                {
                    address = new[] { "tel:+88" + msisdn },
                    senderAddress = "tel:" + SenderAddress,
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
