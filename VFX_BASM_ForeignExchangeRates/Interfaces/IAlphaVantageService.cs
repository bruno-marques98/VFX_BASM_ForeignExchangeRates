using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Interfaces
{
    /// <summary>
    /// Provides operations to obtain foreign exchange rates from the Alpha Vantage service.
    /// Implementations are expected to encapsulate network access, parsing and any caching or retry logic.
    /// </summary>
    public interface IAlphaVantageService
    {
        /// <summary>
        /// Asynchronously retrieves the latest foreign exchange rate for the specified currency pair.
        /// </summary>
        /// <param name="baseCurrency">
        /// The base currency ISO 4217 code (for example, "USD"). The implementation may normalize the value (trim/uppercase).
        /// </param>
        /// <param name="quoteCurrency">
        /// The quote currency ISO 4217 code (for example, "EUR"). The implementation may normalize the value (trim/uppercase).
        /// </param>
        /// <returns>
        /// A <see cref="ForeignExchangeRate"/> describing the exchange rate and related metadata,
        /// or <c>null</c> if the rate could not be obtained (for example, when the pair is invalid or the remote service returns no data).
        /// The result is returned asynchronously as a <see cref="Task{TResult}"/>.
        /// </returns>
        /// <remarks>
        /// Typical implementations perform an HTTP call to the Alpha Vantage API, parse the response and map it to <see cref="ForeignExchangeRate"/>.
        /// Consumers should be prepared to handle transient network exceptions such as <see cref="System.Net.Http.HttpRequestException"/>.
        /// Implementations should also consider rate limiting, retries and local caching to avoid excessive external requests.
        /// </remarks>
        Task<ForeignExchangeRate?> GetExchangeRateAsync(string baseCurrency, string quoteCurrency);
    }
}
