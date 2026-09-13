using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RobiSendPromotionSms
{
    [Table("Robi_Airtel_NumberList")]
    public class RobiAirtelNumberList
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }

        [Column("numbers")]
        [MaxLength(45)]
        public string? Numbers { get; set; }
    }
}
