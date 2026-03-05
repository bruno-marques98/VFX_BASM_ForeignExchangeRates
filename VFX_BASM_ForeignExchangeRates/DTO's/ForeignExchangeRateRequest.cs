namespace VFX_BASM_ForeignExchangeRates.DTO_s
{
    /// <summary>
    /// Represents a request payload for submitting or updating a foreign exchange rate.
    /// Contains the currency pair (base and quote) and the associated bid/ask prices.
    /// </summary>
    public class ForeignExchangeRateRequest
    {
        /// <summary>
        /// The ISO 4217 code of the base currency (e.g., "USD").
        /// This is the currency being quoted against the <see cref="QuoteCurrency"/>.
        /// </summary>
        public string BaseCurrency { get; set; }

        /// <summary>
        /// The ISO 4217 code of the quote currency (e.g., "EUR").
        /// This is the currency used to express the value of the <see cref="BaseCurrency"/>.
        /// </summary>
        public string QuoteCurrency { get; set; }

        /// <summary>
        /// The bid price for the currency pair.
        /// The bid is the price at which the market (or counterparty) is willing to buy the base currency in terms of the quote currency.
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// The ask price for the currency pair.
        /// The ask is the price at which the market (or counterparty) is willing to sell the base currency in terms of the quote currency.
        /// </summary>
        public decimal Ask { get; set; }
    }
}
