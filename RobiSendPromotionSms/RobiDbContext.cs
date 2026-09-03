using Microsoft.EntityFrameworkCore;

namespace RobiSendPromotionSms
{
    /// <summary>
    /// Read access to the RobiDOB database. Credentials match the
    /// RobiDBConnection string in the robi_gamestar_api project.
    /// </summary>
    public class RobiDbContext : DbContext
    {
        public const string ConnectionString =
            "server=db-mysql-adplaybkash-1-do-user-11003141-0.j.db.ondigitalocean.com;" +
            "port=25060;database=RobiDOB;user=developer;password=REDACTED_SEE_APPSETTINGS;" +
            "Allow User Variables=true;";

        public DbSet<RobiAirtelNumberList> RobiAirtelNumberLists { get; set; }

        public DbSet<RobiSmsLog> RobiSmsLogs { get; set; }

        public RobiDbContext() { }

        public RobiDbContext(DbContextOptions<RobiDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(
                    ConnectionString,
                    ServerVersion.AutoDetect(ConnectionString));
            }
        }

        /// <summary>
        /// Returns the numbers whose id falls within the given inclusive range,
        /// mirroring SmsRepository.GetNumbersByIdRangeAsync in the GameStar project.
        /// </summary>
        public async Task<List<string>> GetNumbersByIdRangeAsync()
        {
            return await RobiAirtelNumberLists
                .Select(x => x.Numbers!)
                .ToListAsync();
        }

        /// <summary>
        /// Records one SMS send attempt in tbl_RobiSMSLog.
        /// </summary>
        /// <param name="recieverNumber">The number the message was addressed to.</param>
        /// <param name="smsBody">The message text.</param>
        /// <param name="status">Outcome of the attempt, e.g. "Success" or "Failed".</param>
        /// <param name="senderName">The sender the message went out as.</param>
        public async Task SaveSmsLogAsync(
            string recieverNumber,
            string smsBody,
            string status,
            string senderName = SmsSender.DefaultSenderAddress)
        {
            RobiSmsLogs.Add(new RobiSmsLog
            {
                SenderName = senderName,
                RecieverNumber = recieverNumber,
                SmsBody = smsBody,
                Status = status,
                Timestamp = DateTime.Now
            });

            await SaveChangesAsync();
        }
    }
}
