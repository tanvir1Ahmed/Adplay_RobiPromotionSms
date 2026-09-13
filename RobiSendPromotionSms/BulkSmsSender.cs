namespace RobiSendPromotionSms
{
    public class BulkSmsResult
    {
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }

        public int Batches { get; set; }

        public List<(string Msisdn, string Error)> Failures { get; } = new();
    }

    public class BulkSmsSender
    {
        public const int DefaultBatchSize = 10000;

        public static readonly TimeSpan DefaultBatchPause = TimeSpan.FromHours(2);

        private readonly SmsSender _sender;
        private readonly RobiDbContext _db;

        public BulkSmsSender(SmsSender sender, RobiDbContext db)
        {
            _sender = sender;
            _db = db;
        }

        public async Task<BulkSmsResult> SendToAllAsync(
            string message,
            string senderAddress = SmsSender.DefaultSenderAddress,
            int delayMs = 200,
            IProgress<BulkSmsResult>? progress = null,
            int batchSize = DefaultBatchSize,
            TimeSpan? batchPause = null,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"--------------------------- Application started ---------------------------");
            var pause = batchPause ?? DefaultBatchPause;
            var numbers = await _db.GetNumbersByIdRangeAsync();

            var result = new BulkSmsResult { Total = numbers.Count };

            var sentInBatch = 0;
            int batchCount = 1;
            foreach (var number in numbers)
            {
                
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(number))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    await _sender.SendSms(number, message, senderAddress);
                    result.Sent++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Failures.Add((number, ex.Message));
                }

                progress?.Report(result);

                sentInBatch++;

                if (batchSize > 0 && sentInBatch >= batchSize && result.Sent + result.Failed < result.Total)
                {
                    sentInBatch = 0;
                    result.Batches++;
                    Console.WriteLine($"Batch {batchCount} completed. Pausing for {pause.TotalMinutes} minutes...");
                    await Task.Delay(pause, cancellationToken);
                    batchCount++;
                    continue;
                }

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            Console.WriteLine($"--------------------------- Application Ended ---------------------------");
            return result;
        }
    }
}
