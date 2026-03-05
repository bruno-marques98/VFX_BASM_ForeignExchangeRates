using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VFX_BASM_ForeignExchangeRates.Data;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForeignExchangeRateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlphaVantageService _alphaService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ForeignExchangeRateController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignExchangeRateController"/> class.
        /// </summary>
        /// <param name="context">The <see cref="ApplicationDbContext"/> used to access foreign exchange rates.</param>
        public ForeignExchangeRateController(ApplicationDbContext context, IAlphaVantageService alphaService, 
            IEventPublisher eventPublisher, ILogger<ForeignExchangeRateController> logger)
        {
            _context = context;
            _alphaService = alphaService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        // GET: api/ForeignExchangeRate
        /// <summary>
        /// Retrieves all foreign exchange rates.
        /// </summary>
        /// <returns>
        /// An asynchronous <see cref="ActionResult{IEnumerable}"/> containing a collection of <see cref="ForeignExchangeRate"/>.
        /// Returns HTTP 200 with the list of rates on success.
        /// </returns>
        /// <response code="200">A list of foreign exchange rates.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.ForeignExchangeRate>>> GetAllForeignExchangeRates()
        {
            return await _context.ForeignExchangeRates.ToListAsync();
        }


        // GET: api/ForeignExchangeRate/{id}
        /// <summary>
        /// Retrieves a foreign exchange rate by its identifier.
        /// </summary>
        /// <param name="id">The database identifier of the <see cref="ForeignExchangeRate"/> to retrieve.</param>
        /// <returns>
        /// An asynchronous <see cref="ActionResult{ForeignExchangeRate}"/> containing the rate if found.
        /// Returns HTTP 200 with the rate, or HTTP 404 if not found.
        /// </returns>
        /// <response code="200">The requested foreign exchange rate.</response>
        /// <response code="404">No rate found with the specified identifier.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Models.ForeignExchangeRate>> GetForeignExchangeRateById(int id)
        {
            var rate = await _context.ForeignExchangeRates.FindAsync(id);
            if (rate == null)
            {
                return NotFound();
            }
            return rate;
        }

        // GET: api/ForeignExchangeRate?baseCurrency=USD&quoteCurrency=EUR
        /// <summary>
        /// Retrieves a foreign exchange rate for the specified currency pair.
        /// </summary>
        /// <param name="baseCurrency">The base currency code (e.g., "USD").</param>
        /// <param name="quoteCurrency">The quote currency code (e.g., "EUR").</param>
        /// <returns>
        /// An asynchronous <see cref="ActionResult{ForeignExchangeRate}"/> containing the matching rate.
        /// Returns HTTP 200 with the rate, or HTTP 404 if no matching pair is found.
        /// </returns>
        /// <response code="200">The matching foreign exchange rate.</response>
        /// <response code="404">No rate found for the specified currency pair.</response>
        [HttpGet("{baseCurrency}/{quoteCurrency}")]
        public async Task<ActionResult<Models.ForeignExchangeRate>> GetForeignExchangeRateByCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(quoteCurrency))
                return BadRequest("baseCurrency and quoteCurrency are required.");

            baseCurrency = baseCurrency.ToUpper();
            quoteCurrency = quoteCurrency.ToUpper();

            var rate = await _context.ForeignExchangeRates
                .FirstOrDefaultAsync(x =>
                    x.BaseCurrency == baseCurrency &&
                    x.QuoteCurrency == quoteCurrency);

            if (rate != null)
                return Ok(rate);

            // Not in DB → fetch from third-party
            try
            {
                var externalRate = await _alphaService.GetExchangeRateAsync(baseCurrency, quoteCurrency);

                if (externalRate == null)
                {
                    _logger.LogInformation("AlphaVantage returned no data for {Base}/{Quote}.", baseCurrency, quoteCurrency);
                    return NotFound();
                }

                // Normalize and persist
                externalRate.BaseCurrency = externalRate.BaseCurrency.ToUpper();
                externalRate.QuoteCurrency = externalRate.QuoteCurrency.ToUpper();
                externalRate.Timestamp = DateTime.UtcNow;

                _context.ForeignExchangeRates.Add(externalRate);
                await _context.SaveChangesAsync();

                // Publish event; log failures but do not fail the API response
                try
                {
                    await _eventPublisher.PublishAsync(externalRate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish event for externally fetched rate {Base}/{Quote} (id={Id}).", externalRate.BaseCurrency, externalRate.QuoteCurrency, externalRate.Id);
                }

                return Ok(externalRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching rate from AlphaVantage for {Base}/{Quote}.", baseCurrency, quoteCurrency);
                return Problem(detail: "Error fetching external rate. Please try again later.", statusCode: 503);
            }
        }

        // POST: api/ForeignExchangeRate
        /// <summary>
        /// Creates a new foreign exchange rate.
        /// </summary>
        /// <param name="rate">The <see cref="ForeignExchangeRate"/> to create.</param>
        /// <returns>
        /// An asynchronous <see cref="ActionResult{ForeignExchangeRate}"/> containing the created entity and location header.
        /// Returns HTTP 201 on success, or HTTP 400 for invalid input.
        /// </returns>
        /// <response code="201">The rate was successfully created.</response>
        /// <response code="400">The request data was invalid.</response>
        [HttpPost]
        public async Task<ActionResult<Models.ForeignExchangeRate>> CreateForeignExchangeRate(ForeignExchangeRate rate)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (rate == null)
                return BadRequest("Rate payload is required.");

            rate.BaseCurrency = rate.BaseCurrency.ToUpper();
            rate.QuoteCurrency = rate.QuoteCurrency.ToUpper();
            rate.Timestamp = DateTime.UtcNow;

            // Prevent duplicate currency pair entries
            var exists = await _context.ForeignExchangeRates.AnyAsync(x =>
                x.BaseCurrency == rate.BaseCurrency &&
                x.QuoteCurrency == rate.QuoteCurrency);

            if (exists)
                return Conflict("A rate for this currency pair already exists.");


            // Publish event; log failures but do not block the client response
            try
            {
                await _eventPublisher.PublishAsync(rate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish event for newly created rate {Base}/{Quote} (id={Id}).", rate.BaseCurrency, rate.QuoteCurrency, rate.Id);
            }

            return CreatedAtAction(nameof(GetForeignExchangeRateById), new { id = rate.Id }, rate);
        }

        // PUT: api/ForeignExchangeRate/{id}
        /// <summary>
        /// Updates an existing foreign exchange rate.
        /// </summary>
        /// <param name="id">The identifier of the rate to update.</param>
        /// <param name="updatedRate">The updated <see cref="ForeignExchangeRate"/> payload.</param>
        /// <returns>
        /// An asynchronous <see cref="IActionResult"/>. Returns HTTP 204 on success, HTTP 400 if the id does not match,
        /// or HTTP 404 if the rate does not exist.
        /// </returns>
        /// <response code="204">The rate was successfully updated.</response>
        /// <response code="400">The supplied id does not match the payload.</response>
        /// <response code="404">No rate found with the specified identifier.</response>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateForeignExchangeRate(int id, ForeignExchangeRate updatedRate)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != updatedRate.Id)
                return BadRequest();

            var existingRate = await _context.ForeignExchangeRates.FindAsync(id);
            if (existingRate == null)
                return NotFound();

            existingRate.BaseCurrency = updatedRate.BaseCurrency.ToUpper();
            existingRate.QuoteCurrency = updatedRate.QuoteCurrency.ToUpper();
            existingRate.Bid = updatedRate.Bid;
            existingRate.Ask = updatedRate.Ask;
            existingRate.Timestamp = DateTime.UtcNow;
            _context.Entry(existingRate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating rate id={Id}.", id);
                return Problem(detail: "Concurrency error while updating rate.", statusCode: 409);
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes the <see cref="ForeignExchangeRate"/> with the specified identifier.
        /// </summary>
        /// <param name="id">The database identifier of the foreign exchange rate to delete.</param>
        /// <returns>
        /// An asynchronous task that yields an <see cref="IActionResult"/>.
        /// Returns <see cref="NoContentResult"/> (HTTP 204) when the deletion succeeds.
        /// Returns <see cref="NotFoundResult"/> (HTTP 404) if no entity with the provided <paramref name="id"/> exists.
        /// Returns <see cref="BadRequestResult"/> (HTTP 400) when the request is invalid.
        /// </returns>
        /// <remarks>
        /// The method will remove the entity from the application's DbContext and persist changes
        /// to the underlying database by calling <c>SaveChangesAsync()</c>. This operation is
        /// irreversible for the deleted record.
        /// </remarks>
        /// <response code="204">The rate was successfully deleted.</response>
        /// <response code="404">A rate with the specified id was not found.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForeignExchangeRate(int id)
        {
            var rate = await _context.ForeignExchangeRates.FindAsync(id);
            if (rate == null)
                return NotFound();
            _context.ForeignExchangeRates.Remove(rate);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
