using System.Text.Json;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Services
{
    /// <summary>
    /// Provides methods to query the Alpha Vantage API for foreign exchange rates.
    /// </summary>
    /// <remarks>
    /// This service uses an injected <see cref="HttpClient"/> for HTTP calls and reads the Alpha Vantage API key
    /// from configuration using the key "AlphaVantage:457YJJFZWML8PF1D". The <see cref="GetExchangeRateAsync(string,string)"/>
    /// method returns a <see cref="ForeignExchangeRate"/> populated from the API response or <c>null</c> when the
    /// HTTP response is not successful.
    /// </remarks>
    public class AlphaVantageService : IAlphaVantageService
    {
        /// <summary>
        /// The <see cref="HttpClient"/> used to send HTTP requests to the Alpha Vantage API.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// The application configuration used to retrieve settings such as the Alpha Vantage API key.
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaVantageService"/> class.
        /// </summary>
        /// <param name="httpClient">
        /// The <see cref="HttpClient"/> used to send HTTP requests to the Alpha Vantage API.
        /// This client is expected to be managed by dependency injection (e.g., registered via
        /// <c>IHttpClientFactory</c> or as a singleton/scoped service).
        /// </param>
        /// <param name="config">
        /// The application <see cref="IConfiguration"/> used to retrieve configuration values such as the Alpha Vantage API key.
        /// The API key is read from configuration at the key "AlphaVantage:457YJJFZWML8PF1D".
        /// </param>
        public AlphaVantageService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        /// <summary>
        /// Retrieves the live foreign exchange rate for a currency pair from the Alpha Vantage API.
        /// </summary>
        /// <param name="baseCurrency">The ISO 4217 currency code to convert from (e.g., "USD").</param>
        /// <param name="quoteCurrency">The ISO 4217 currency code to convert to (e.g., "EUR").</param>
        /// <returns>
        /// A <see cref="ForeignExchangeRate"/> populated with bid, ask and timestamp information when the API call succeeds;
        /// otherwise <c>null</c> when the HTTP response status is not successful.
        /// </returns>
        /// <remarks>
        /// The method constructs a query to the Alpha Vantage endpoint with function=CURRENCY_EXCHANGE_RATE.
        /// It expects the JSON response to contain a top-level property named "Realtime Currency Exchange Rate"
        /// and the numeric string properties "8. Bid Price" and "9. Ask Price".
        /// </remarks>
        /// <exception cref="HttpRequestException">Thrown when the underlying <see cref="HttpClient"/> encounters a network error.</exception>
        /// <exception cref="JsonException">Thrown when the API response cannot be parsed as JSON.</exception>
        /// <exception cref="FormatException">Thrown when the bid/ask string values cannot be parsed to <see cref="decimal"/>.</exception>
        public async Task<ForeignExchangeRate?> GetExchangeRateAsync(string baseCurrency, string quoteCurrency)
        {
            var apiKey = _config["AlphaVantage:ApiKey"];

            var url =
                $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE" +
                $"&from_currency={baseCurrency}&to_currency={quoteCurrency}&apikey={apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("Realtime Currency Exchange Rate", out var rateNode))
                return null;

            var bid = decimal.Parse(
                rateNode.GetProperty("8. Bid Price").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);

            var ask = decimal.Parse(
                rateNode.GetProperty("9. Ask Price").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);

            return new ForeignExchangeRate
            {
                BaseCurrency = baseCurrency,
                QuoteCurrency = quoteCurrency,
                Bid = bid,
                Ask = ask,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
