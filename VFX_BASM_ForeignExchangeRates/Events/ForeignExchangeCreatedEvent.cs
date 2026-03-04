namespace VFX_BASM_ForeignExchangeRates.Events
{
    public record ForeignExchangeCreatedEvent(string BaseCurrency, string QuoteCurrency, decimal Bid, decimal Ask, DateTime Timestamp);
}
