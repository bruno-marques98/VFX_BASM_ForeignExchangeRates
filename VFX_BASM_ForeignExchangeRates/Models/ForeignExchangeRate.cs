using System.ComponentModel.DataAnnotations;

namespace VFX_BASM_ForeignExchangeRates.Models
{
    public class ForeignExchangeRate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string BaseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string QuoteCurrency { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Bid { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Ask { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
