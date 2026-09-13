using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RobiSendPromotionSms
{
    [Table("tbl_RobiSMSLog")]
    public class RobiSmsLog
    {
        [Column("Id")]
        [Key]
        public int Id { get; set; }

        [Column("sender_name")]
        [Required]
        [MaxLength(45)]
        public string SenderName { get; set; } = string.Empty;

        [Column("RecieverNumber")]
        public string? RecieverNumber { get; set; }

        [Column("SMSBody")]
        public string? SmsBody { get; set; }

        [Column("status")]
        [Required]
        [MaxLength(45)]
        public string Status { get; set; } = string.Empty;

        [Column("Timestamp")]
        public DateTime? Timestamp { get; set; } = DateTime.Now;
    }
}
