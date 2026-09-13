using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RobiSendPromotionSms
{
    public class SmsSender
    {
        private readonly HttpClient _httpClient;

        public const string DefaultSenderAddress = "DARUN_OFFER";
        public const string DefaultSenderName = "Gamestar";

        private const string SendSmsUrl =
            "https://robigamestarapi.gamestar.team/sms/send-sms";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SmsSender(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendSms(
            string msisdn,
            string message,
            string senderAddress = DefaultSenderAddress,
            string senderName = DefaultSenderName)
        {
            if (msisdn.StartsWith("88"))
            {
                msisdn = msisdn.Substring(2);
            }

            var body = new
            {
                senderName = senderAddress,
                receipientNumber = msisdn,
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

            if (responseContent.Contains("\"failed\"", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"SMS API returned failed status: {responseContent}");
            }

            return responseContent;
        }
    }
}
