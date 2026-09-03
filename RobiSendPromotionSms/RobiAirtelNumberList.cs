using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RobiSendPromotionSms
{
    /// <summary>
    /// A recipient number in the promotional send list.
    /// Maps to Robi_Airtel_NumberList in the RobiDOB database.
    /// </summary>
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
