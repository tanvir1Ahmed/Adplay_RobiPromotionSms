namespace RobiSendPromotionSms
{
    /// <summary>
    /// Drives a promotional send: builds the dependencies, runs the bulk send,
    /// and reports progress and the final summary to the console.
    /// </summary>
    public class PromotionSmsRunner
    {
        /// <summary>Pause between sends, in milliseconds.</summary>
        public int DelayMs { get; set; } = 200;

        /// <summary>How often to print a progress line, in messages.</summary>
        public int ProgressInterval { get; set; } = 25;

        /// <summary>
        /// Runs the send end to end. Ctrl+C stops it cleanly and still prints the
        /// summary. Returns the result, or null if the run was cancelled before
        /// completing.
        /// </summary>
        /// <param name="message">The text sent to every number in the list.</param>
        /// <param name="senderAddress">Short code the messages are sent from.</param>
        public async Task<BulkSmsResult?> RunAsync(
            string message,
            string senderAddress = SmsSender.DefaultSenderAddress)
        {
            using var httpClient = new HttpClient();
            var sender = new SmsSender(httpClient);

            using var db = new RobiDbContext();
            var bulkSender = new BulkSmsSender(sender, db);

            using var cts = new CancellationTokenSource();
            ConsoleCancelEventHandler onCancel = (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nStopping...");
            };
            Console.CancelKeyPress += onCancel;

            try
            {
                Console.WriteLine("Sending to ALL numbers...");

                var result = await bulkSender.SendToAllAsync(
                    message, senderAddress, DelayMs, CreateProgress(), cts.Token);

                PrintSummary(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Run cancelled.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                return null;
            }
            finally
            {
                Console.CancelKeyPress -= onCancel;
            }
        }

        private IProgress<BulkSmsResult> CreateProgress()
        {
            return new Progress<BulkSmsResult>(r =>
            {
                var done = r.Sent + r.Failed;
                if (done % ProgressInterval == 0 || done == r.Total)
                {
                    Console.WriteLine($"  {done}/{r.Total}  sent={r.Sent} failed={r.Failed}");
                }
            });
        }

        private static void PrintSummary(BulkSmsResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"Total:   {result.Total}");
            Console.WriteLine($"Sent:    {result.Sent}");
            Console.WriteLine($"Failed:  {result.Failed}");
            Console.WriteLine($"Skipped: {result.Skipped}");

            if (result.Failures.Count > 0)
            {
                Console.WriteLine("\nFirst failures:");
                foreach (var (msisdn, error) in result.Failures.Take(10))
                {
                    Console.WriteLine($"  {msisdn}: {error}");
                }
            }
        }
    }
}
