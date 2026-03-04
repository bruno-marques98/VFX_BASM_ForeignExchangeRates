using System.ComponentModel.DataAnnotations;

namespace VFX_BASM_ForeignExchangeRates.Models
{
    /// <summary>
    /// Represents a foreign exchange rate between two currencies.
    /// Contains the base and quote currency codes, bid and ask prices, and the timestamp when the rate was recorded.
    /// </summary>
    public class ForeignExchangeRate
    {
        /// <summary>
        /// Gets or sets the unique identifier for the exchange rate record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the three-letter ISO currency code for the base currency.
        /// </summary>
        /// <remarks>
        /// This property is required and limited to 3 characters by data annotations.
        /// </remarks>
        [Required]
        [StringLength(3)]
        public string BaseCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the three-letter ISO currency code for the quote currency.
        /// </summary>
        /// <remarks>
        /// This property is required and limited to 3 characters by data annotations.
        /// </remarks>
        [Required]
        [StringLength(3)]
        public string QuoteCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the bid price for the currency pair.
        /// </summary>
        /// <remarks>
        /// Must be a non-negative decimal value. Validation is enforced via the <see cref="RangeAttribute"/>.
        /// </remarks>
        [Range(0, double.MaxValue)]
        public decimal Bid { get; set; }

        /// <summary>
        /// Gets or sets the ask price for the currency pair.
        /// </summary>
        /// <remarks>
        /// Must be a non-negative decimal value. Validation is enforced via the <see cref="RangeAttribute"/>.
        /// </remarks>
        [Range(0, double.MaxValue)]
        public decimal Ask { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp indicating when the rate was recorded.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="System.DateTime.UtcNow"/> at creation time.
        /// </remarks>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
