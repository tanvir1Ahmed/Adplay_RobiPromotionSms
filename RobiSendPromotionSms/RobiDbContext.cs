using Microsoft.EntityFrameworkCore;

namespace RobiSendPromotionSms
{
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

        public async Task<List<string>> GetNumbersByIdRangeAsync()
        {
            return await RobiAirtelNumberLists
            .OrderByDescending(x => x.Id)
            .Select(x => x.Numbers!)
            .ToListAsync();
        }

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
