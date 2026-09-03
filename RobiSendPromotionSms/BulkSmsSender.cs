namespace RobiSendPromotionSms
{
    /// <summary>
    /// Outcome of a bulk send.
    /// </summary>
    public class BulkSmsResult
    {
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }

        /// <summary>Numbers that failed, with the reason, for retry or inspection.</summary>
        public List<(string Msisdn, string Error)> Failures { get; } = new();
    }

    /// <summary>
    /// Sends one message to every number in Robi_Airtel_NumberList.
    /// </summary>
    public class BulkSmsSender
    {
        private readonly SmsSender _sender;
        private readonly RobiDbContext _db;

        public BulkSmsSender(SmsSender sender, RobiDbContext db)
        {
            _sender = sender;
            _db = db;
        }

        /// <summary>
        /// Sends <paramref name="message"/> to every number in the table.
        /// </summary>
        /// <param name="message">The text to send.</param>
        /// <param name="senderAddress">Short code the messages are sent from.</param>
        /// <param name="delayMs">
        /// Pause between sends, to stay under the gateway's rate limit.
        /// </param>
        /// <param name="progress">Called after each send with the running result.</param>
        /// <param name="cancellationToken">Stops the run early; partial results are returned.</param>
        public async Task<BulkSmsResult> SendToAllAsync(
            string message,
            string senderAddress = SmsSender.DefaultSenderAddress,
            int delayMs = 200,
            IProgress<BulkSmsResult>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var numbers = await _db.GetNumbersByIdRangeAsync();

            var result = new BulkSmsResult { Total = numbers.Count };

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
                    await SaveLogAsync(number, message, "Success", senderAddress);
                }
                catch (Exception ex)
                {
                    // One bad number must not abort the run.
                    result.Failed++;
                    result.Failures.Add((number, ex.Message));
                    await SaveLogAsync(number, message, "Failed", senderAddress);
                }

                progress?.Report(result);

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            return result;
        }

        /// <summary>
        /// Writes a log row, swallowing any error. A logging failure must not
        /// abort a run that is otherwise sending successfully.
        /// </summary>
        private async Task SaveLogAsync(
            string number, string message, string status, string senderAddress)
        {
            try
            {
                await _db.SaveSmsLogAsync(number, message, status, senderAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [log failed for {number}: {ex.Message}]");
            }
        }
    }
}
