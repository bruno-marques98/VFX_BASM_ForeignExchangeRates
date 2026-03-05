namespace VFX_BASM_ForeignExchangeRates.Events
{
    /// <summary>
    /// Event published when a new foreign-exchange quote is created.
    /// Represents a snapshot of a currency pair including bid/ask prices and the time the quote was produced.
    /// </summary>
    /// <param name="BaseCurrency">The ISO 4217 code of the base currency (for example, "USD").</param>
    /// <param name="QuoteCurrency">The ISO 4217 code of the quote currency (for example, "EUR").</param>
    /// <param name="Bid">The bid price — the price at which the market is willing to buy the base currency.</param>
    /// <param name="Ask">The ask price — the price at which the market is willing to sell the base currency.</param>
    /// <param name="Timestamp">The date and time when the quote was produced.</param>
    public record ForeignExchangeCreatedEvent(string BaseCurrency, string QuoteCurrency, decimal Bid, decimal Ask, DateTime Timestamp);
}
