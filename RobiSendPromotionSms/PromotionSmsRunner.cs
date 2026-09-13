namespace RobiSendPromotionSms
{
    public class PromotionSmsRunner
    {
        static PromotionSmsRunner()
        {
            Console.CancelKeyPress += (_, e) => e.Cancel = true;
        }

        public int DelayMs { get; set; } = 200;

        public int BatchSize { get; set; } = BulkSmsSender.DefaultBatchSize;

        public TimeSpan BatchPause { get; set; } = BulkSmsSender.DefaultBatchPause;

        public async Task<BulkSmsResult?> RunAsync(
            string message,
            string senderAddress = SmsSender.DefaultSenderAddress)
        {
            using var httpClient = new HttpClient();
            var sender = new SmsSender(httpClient);

            using var db = new RobiDbContext();
            var bulkSender = new BulkSmsSender(sender, db);

            try
            {
                return await bulkSender.SendToAllAsync(
                    message, senderAddress, DelayMs, null,
                    BatchSize, BatchPause);
            }
            catch
            {
                return null;
            }
        }
    }
}
