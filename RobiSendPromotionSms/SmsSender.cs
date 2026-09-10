using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RobiSendPromotionSms
{
    /// <summary>
    /// Sends an SMS by calling the deployed GameStar /sms/send-sms endpoint.
    /// That API owns the Robi APIGate credentials and token handling, so nothing
    /// here needs to authenticate against Robi directly.
    /// </summary>
    public class SmsSender
    {
        private readonly HttpClient _httpClient;

        public const string DefaultSenderAddress = "DARUN_OFFER";
        public const string DefaultSenderName = "Gamestar";

        private const string SendSmsUrl =
            "https://robigamestarapi.gamestar.team/sms/send-sms";

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
        /// Sends one SMS. Returns the raw API response.
        /// </summary>
        /// <param name="msisdn">
        /// Recipient number. A leading 88 is stripped, so both 8801852956967 and
        /// 01852956967 are accepted.
        /// </param>
        /// <param name="message">Message text.</param>
        /// <param name="senderAddress">
        /// Sender the message appears from, e.g. DARUN_OFFER.
        /// </param>
        /// <param name="senderName">Sender name recorded with the send.</param>
        public async Task<string> SendSms(
            string msisdn,
            string message,
            string senderAddress = DefaultSenderAddress,
            string senderName = DefaultSenderName)
        {
            if (msisdn.StartsWith("88"))
            {
                msisdn = msisdn.Substring(2); // "01852956967"
            }

            var body = new
            {
                senderName = senderAddress,
                receipientNumber = msisdn,
                //receipientNumber = msisdn,
                message = message,
                senderAddress = senderAddress
            };

            var jsonBody = JsonSerializer.Serialize(body, SerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, SendSmsUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SMS API Error: {response.StatusCode} - {responseContent}");
            }

            // The endpoint answers 200 with {"status":"success"} or
            // {"status":"failed"}, so a 2xx alone does not mean the send worked.
            if (responseContent.Contains("\"failed\"", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"SMS API returned failed status: {responseContent}");
            }

            return responseContent;
        }
    }
}
