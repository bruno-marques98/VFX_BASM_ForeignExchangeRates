using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VFX_BASM_ForeignExchangeRates.Controllers;
using VFX_BASM_ForeignExchangeRates.Data;
using VFX_BASM_ForeignExchangeRates.DTO_s;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;
using Xunit;

namespace ForeignExchangeRatesTests
{
    /// <summary>
    /// Unit tests for <see cref="ForeignExchangeRateController"/>.
    /// </summary>
    public class ForeignExchangeRateTests
    {
        /// <summary>
        /// Creates an <see cref="ApplicationDbContext"/> configured to use an in-memory database.
        /// </summary>
        /// <param name="dbName">A unique name for the in-memory database used by the test.</param>
        /// <returns>A new <see cref="ApplicationDbContext"/> instance using the specified in-memory database.</returns>
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        // Helper subclass to simulate SaveChangesAsync throwing concurrency exceptions
        /// <summary>
        /// Test-specific <see cref="ApplicationDbContext"/> that can be instructed to throw a
        /// <see cref="DbUpdateConcurrencyException"/> from <see cref="SaveChangesAsync"/> to
        /// exercise concurrency-handling code paths.
        /// </summary>
        private class TestDbContext : ApplicationDbContext
        {
            /// <summary>
            /// When true, <see cref="SaveChangesAsync(CancellationToken)"/> will throw a <see cref="DbUpdateConcurrencyException"/>.
            /// </summary>
            public bool ThrowOnSave { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="TestDbContext"/> class.
            /// </summary>
            /// <param name="options">The options to configure the underlying <see cref="ApplicationDbContext"/>.</param>
            public TestDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            /// <summary>
            /// Overrides <see cref="ApplicationDbContext.SaveChangesAsync(CancellationToken)"/> to optionally throw
            /// a <see cref="DbUpdateConcurrencyException"/> when <see cref="ThrowOnSave"/> is true.
            /// </summary>
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (ThrowOnSave)
                    throw new DbUpdateConcurrencyException();
                return base.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Verifies that <see cref="ForeignExchangeRateController.GetAllForeignExchangeRates"/> returns all persisted rates.
        /// </summary>
        [Fact]
        public async Task GetAllForeignExchangeRates_ReturnsAllRates()
        {
            using var context = CreateInMemoryContext(nameof(GetAllForeignExchangeRates_ReturnsAllRates));
            context.ForeignExchangeRates.AddRange(new[]
            {
                new ForeignExchangeRate { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1m, Ask = 1.1m, Timestamp = DateTime.UtcNow },
                new ForeignExchangeRate { BaseCurrency = "GBP", QuoteCurrency = "EUR", Bid = 1.2m, Ask = 1.3m, Timestamp = DateTime.UtcNow }
            });
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetAllForeignExchangeRates();

            var list = Assert.IsAssignableFrom<IEnumerable<ForeignExchangeRate>>(result.Value);
            Assert.Equal(2, list.Count());
        }

        /// <summary>
        /// Verifies that requesting a rate by id returns the entity when it exists.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateById_ReturnsRate_WhenExists()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateById_ReturnsRate_WhenExists));
            var rate = new ForeignExchangeRate { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1m, Ask = 1.1m, Timestamp = DateTime.UtcNow };
            context.ForeignExchangeRates.Add(rate);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var action = await controller.GetForeignExchangeRateById(rate.Id);

            Assert.NotNull(action.Value);
            Assert.Equal(rate.Id, action.Value.Id);
        }

        /// <summary>
        /// Verifies that requesting a missing rate by id returns <see cref="NotFoundResult"/>.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateById_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateById_ReturnsNotFound_WhenMissing));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var action = await controller.GetForeignExchangeRateById(123);

            Assert.IsType<NotFoundResult>(action.Result);
        }

        /// <summary>
        /// Verifies that fetching a currency pair returns an entity from the database when present.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_ReturnsFromDb_WhenExists()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateByCurrencyPair_ReturnsFromDb_WhenExists));
            var rate = new ForeignExchangeRate { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1m, Ask = 1.1m, Timestamp = DateTime.UtcNow };
            context.ForeignExchangeRates.Add(rate);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("usd", "eur");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(ok.Value);
            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("EUR", returned.QuoteCurrency);
        }

        /// <summary>
        /// Verifies that when a currency pair is not present in the DB, the controller calls the external service,
        /// persists the returned rate, and publishes an event.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_FetchesExternalAndSaves_WhenNotInDb()
        {
            var dbName = nameof(GetForeignExchangeRateByCurrencyPair_FetchesExternalAndSaves_WhenNotInDb);
            using var context = CreateInMemoryContext(dbName);

            var external = new ForeignExchangeRate
            {
                BaseCurrency = "USD",
                QuoteCurrency = "JPY",
                Bid = 110.5m,
                Ask = 111.0m
            };

            var alphaMock = new Mock<IAlphaVantageService>();
            alphaMock.Setup(x => x.GetExchangeRateAsync("USD", "JPY"))
                .ReturnsAsync(external);

            var eventMock = new Mock<IEventPublisher>();
            eventMock.Setup(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>())).Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("usd", "jpy");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(ok.Value);

            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("JPY", returned.QuoteCurrency);

            // Ensure persisted
            var persisted = await context.ForeignExchangeRates
                .FirstOrDefaultAsync(x => x.BaseCurrency == "USD" && x.QuoteCurrency == "JPY");
            Assert.NotNull(persisted);

            eventMock.Verify(x => x.PublishAsync(It.Is<ForeignExchangeRate>(r => r.BaseCurrency == "USD" && r.QuoteCurrency == "JPY")), Times.Once);
        }

        /// <summary>
        /// Verifies that when the external service returns null for a currency pair, the controller returns NotFound.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_ReturnsNotFound_WhenExternalMissing()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateByCurrencyPair_ReturnsNotFound_WhenExternalMissing));

            var alphaMock = new Mock<IAlphaVantageService>();
            alphaMock.Setup(x => x.GetExchangeRateAsync("ABC", "DEF"))
                .ReturnsAsync((ForeignExchangeRate?)null);

            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("ABC", "DEF");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Verifies controller returns BadRequest when base or quote currency is null/whitespace.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_ReturnsBadRequest_WhenParametersMissing()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateByCurrencyPair_ReturnsBadRequest_WhenParametersMissing));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            // null baseCurrency
            var resultNullBase = await controller.GetForeignExchangeRateByCurrencyPair(null, "EUR");
            Assert.IsType<BadRequestObjectResult>(resultNullBase.Result);

            // whitespace quoteCurrency
            var resultWhitespaceQuote = await controller.GetForeignExchangeRateByCurrencyPair("USD", "   ");
            Assert.IsType<BadRequestObjectResult>(resultWhitespaceQuote.Result);
        }

        /// <summary>
        /// Verifies that when the external service throws an exception the controller responds with 503.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_ReturnsServiceUnavailable_OnExternalException()
        {
            using var context = CreateInMemoryContext(nameof(GetForeignExchangeRateByCurrencyPair_ReturnsServiceUnavailable_OnExternalException));
            var alphaMock = new Mock<IAlphaVantageService>();
            alphaMock.Setup(x => x.GetExchangeRateAsync("USD", "EUR"))
                .ThrowsAsync(new Exception("network failure"));

            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("USD", "EUR");

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, objectResult.StatusCode);
        }

        /// <summary>
        /// Verifies that controller still returns OK and persists the rate when publishing the event fails.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_PersistsAndReturnsOk_WhenPublishThrows()
        {
            var dbName = nameof(GetForeignExchangeRateByCurrencyPair_PersistsAndReturnsOk_WhenPublishThrows);
            using var context = CreateInMemoryContext(dbName);

            var external = new ForeignExchangeRate
            {
                BaseCurrency = "usd",
                QuoteCurrency = "jpy",
                Bid = 110.5m,
                Ask = 111.0m
            };

            var alphaMock = new Mock<IAlphaVantageService>();
            alphaMock.Setup(x => x.GetExchangeRateAsync("USD", "JPY"))
                .ReturnsAsync(external);

            var eventMock = new Mock<IEventPublisher>();
            eventMock.Setup(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>()))
                .ThrowsAsync(new Exception("publish failure"));

            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("usd", "jpy");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(ok.Value);

            // normalized to uppercase in response
            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("JPY", returned.QuoteCurrency);

            // persisted despite publish failure
            var persisted = await context.ForeignExchangeRates
                .FirstOrDefaultAsync(x => x.BaseCurrency == "USD" && x.QuoteCurrency == "JPY");
            Assert.NotNull(persisted);

            eventMock.Verify(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>()), Times.Once);
        }

        /// <summary>
        /// Verifies that external rates with lowercase currency codes are normalized before persisting.
        /// </summary>
        [Fact]
        public async Task GetForeignExchangeRateByCurrencyPair_NormalizesExternalCurrencies_WhenLowercase()
        {
            var dbName = nameof(GetForeignExchangeRateByCurrencyPair_NormalizesExternalCurrencies_WhenLowercase);
            using var context = CreateInMemoryContext(dbName);

            var external = new ForeignExchangeRate
            {
                BaseCurrency = "usd",
                QuoteCurrency = "jpy",
                Bid = 110.5m,
                Ask = 111.0m
            };

            var alphaMock = new Mock<IAlphaVantageService>();
            alphaMock.Setup(x => x.GetExchangeRateAsync("USD", "JPY"))
                .ReturnsAsync(external);

            var eventMock = new Mock<IEventPublisher>();
            eventMock.Setup(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>())).Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.GetForeignExchangeRateByCurrencyPair("usd", "jpy");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(ok.Value);

            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("JPY", returned.QuoteCurrency);

            var persisted = await context.ForeignExchangeRates
                .FirstOrDefaultAsync(x => x.BaseCurrency == "USD" && x.QuoteCurrency == "JPY");
            Assert.NotNull(persisted);
            Assert.Equal("USD", persisted.BaseCurrency);
            Assert.Equal("JPY", persisted.QuoteCurrency);
        }

        /// <summary>
        /// Verifies that creating a duplicate foreign exchange rate returns a conflict response.
        /// </summary>
        [Fact]
        public async Task CreateForeignExchangeRate_ReturnsConflict_WhenDuplicateExists()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_ReturnsConflict_WhenDuplicateExists));
            var existing = new ForeignExchangeRate { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1m, Ask = 1.1m, Timestamp = DateTime.UtcNow };
            context.ForeignExchangeRates.Add(existing);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var newRate = new ForeignExchangeRateRequest { BaseCurrency = "usd", QuoteCurrency = "eur", Bid = 1.01m, Ask = 1.11m };

            var action = await controller.CreateForeignExchangeRate(newRate);

            Assert.IsType<ConflictObjectResult>(action.Result);
        }

        /// <summary>
        /// Verifies that a successful create publishes an event and returns the created entity.
        /// </summary>
        [Fact]
        public async Task CreateForeignExchangeRate_PublishesEvent_OnSuccess()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_PublishesEvent_OnSuccess));

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            eventMock.Setup(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>())).Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var newRate = new ForeignExchangeRateRequest { BaseCurrency = "usd", QuoteCurrency = "cad", Bid = 1.25m, Ask = 1.27m };

            var action = await controller.CreateForeignExchangeRate(newRate);

            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(created.Value);
            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("CAD", returned.QuoteCurrency);

            eventMock.Verify(x => x.PublishAsync(It.Is<ForeignExchangeRate>(r => r.BaseCurrency == "USD" && r.QuoteCurrency == "CAD")), Times.Once);
        }

        /// <summary>
        /// Verifies that Create returns BadRequest when controller's ModelState is invalid.
        /// </summary>
        [Fact]
        public async Task CreateForeignExchangeRate_ReturnsBadRequest_WhenModelStateInvalid()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_ReturnsBadRequest_WhenModelStateInvalid));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);
            controller.ModelState.AddModelError("BaseCurrency", "Required");

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "",
                QuoteCurrency = "EUR",
                Bid = 1m,
                Ask = 1.1m
            };

            var action = await controller.CreateForeignExchangeRate(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
            Assert.NotNull(badRequest.Value);
        }

        /// <summary>
        /// Verifies that Create returns BadRequest when the payload is null.
        /// The controller is expected to return a specific error message for null payloads.
        /// </summary>
        [Fact]
        public async Task CreateForeignExchangeRate_ReturnsBadRequest_WhenPayloadIsNull()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_ReturnsBadRequest_WhenPayloadIsNull));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var action = await controller.CreateForeignExchangeRate(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
            // Controller returns a specific error message for null payloads
            Assert.Equal("Rate payload is required.", badRequest.Value);
        }

        /// <summary>
        /// Verifies that Create returns BadRequest when Ask is less than Bid (invalid pricing).
        /// </summary>
        [Fact]
        public async Task CreateForeignExchangeRate_ReturnsBadRequest_WhenAskLessThanBid()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_ReturnsBadRequest_WhenAskLessThanBid));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.50m,
                Ask = 1.40m // Ask < Bid → invalid
            };

            var action = await controller.CreateForeignExchangeRate(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
            Assert.Equal("Ask price must be greater than or equal to Bid.", badRequest.Value);
        }

        /// <summary>
        /// Verifies that Update returns BadRequest when Ask is less than Bid.
        /// </summary>
        [Fact]
        public async Task UpdateForeignExchangeRate_ReturnsBadRequest_WhenAskLessThanBid()
        {
            var dbName = nameof(UpdateForeignExchangeRate_ReturnsBadRequest_WhenAskLessThanBid);
            using var context = CreateInMemoryContext(dbName);

            // seed an existing entity
            var existing = new ForeignExchangeRate
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.00m,
                Ask = 1.10m
            };
            context.ForeignExchangeRates.Add(existing);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 2.00m,
                Ask = 1.90m // Ask < Bid → invalid
            };

            var result = await controller.UpdateForeignExchangeRate(existing.Id, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Ask price must be greater than or equal to Bid.", badRequest.Value);
        }

        /// <summary>
        /// Verifies that Update returns BadRequest when the controller's ModelState is invalid.
        /// </summary>
        [Fact]
        public async Task UpdateForeignExchangeRate_ReturnsBadRequest_WhenModelStateInvalid()
        {
            using var context = CreateInMemoryContext(nameof(UpdateForeignExchangeRate_ReturnsBadRequest_WhenModelStateInvalid));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);
            controller.ModelState.AddModelError("BaseCurrency", "Required");

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "",
                QuoteCurrency = "EUR",
                Bid = 1m,
                Ask = 1.1m
            };

            var result = await controller.UpdateForeignExchangeRate(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value); // ModelState details included
        }

        /// <summary>
        /// Verifies that Update returns NotFound when the entity to update does not exist.
        /// </summary>
        [Fact]
        public async Task UpdateForeignExchangeRate_ReturnsNotFound_WhenEntityMissing()
        {
            using var context = CreateInMemoryContext(nameof(UpdateForeignExchangeRate_ReturnsNotFound_WhenEntityMissing));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1m,
                Ask = 1.1m
            };

            var result = await controller.UpdateForeignExchangeRate(42, request);

            Assert.IsType<NotFoundResult>(result);
        }

        /// <summary>
        /// Verifies that Update applies changes to an existing entity when the request is valid.
        /// </summary>
        [Fact]
        public async Task UpdateForeignExchangeRate_UpdatesEntity_WhenValid()
        {
            var dbName = nameof(UpdateForeignExchangeRate_UpdatesEntity_WhenValid);
            using var context = CreateInMemoryContext(dbName);

            var existing = new ForeignExchangeRate
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.00m,
                Ask = 1.10m
            };
            context.ForeignExchangeRates.Add(existing);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "usd",
                QuoteCurrency = "eur",
                Bid = 1.02m,
                Ask = 1.12m
            };

            var result = await controller.UpdateForeignExchangeRate(existing.Id, request);

            Assert.IsType<NoContentResult>(result);

            var fromDb = await context.ForeignExchangeRates.FindAsync(existing.Id);
            Assert.Equal("USD", fromDb.BaseCurrency);
            Assert.Equal("EUR", fromDb.QuoteCurrency);
            Assert.Equal(1.02m, fromDb.Bid);
            Assert.Equal(1.12m, fromDb.Ask);
        }

        /// <summary>
        /// Verifies that when SaveChanges throws a concurrency exception during update, the controller returns a 409 conflict.
        /// Uses <see cref="TestDbContext"/> to simulate the exception.
        /// </summary>
        [Fact]
        public async Task UpdateForeignExchangeRate_ReturnsConflict_OnConcurrencyException()
        {
            var dbName = nameof(UpdateForeignExchangeRate_ReturnsConflict_OnConcurrencyException);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            // seed data using a normal context
            using (var seed = new ApplicationDbContext(options))
            {
                seed.ForeignExchangeRates.Add(new ForeignExchangeRate
                {
                    BaseCurrency = "USD",
                    QuoteCurrency = "EUR",
                    Bid = 1.00m,
                    Ask = 1.10m
                });
                await seed.SaveChangesAsync();
            }

            // use TestDbContext that throws on SaveChangesAsync
            using var context = new TestDbContext(options) { ThrowOnSave = true };

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            // load existing id
            var existing = await context.ForeignExchangeRates.FirstAsync();

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.05m,
                Ask = 1.15m
            };

            var result = await controller.UpdateForeignExchangeRate(existing.Id, request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, objectResult.StatusCode);
        }

        /// <summary>
        /// Verifies that Delete removes an existing rate and returns NoContent.
        /// </summary>
        [Fact]
        public async Task DeleteForeignExchangeRate_RemovesRate_WhenExists()
        {
            using var context = CreateInMemoryContext(nameof(DeleteForeignExchangeRate_RemovesRate_WhenExists));
            var rate = new ForeignExchangeRate { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1m, Ask = 1.1m, Timestamp = DateTime.UtcNow };
            context.ForeignExchangeRates.Add(rate);
            await context.SaveChangesAsync();

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.DeleteForeignExchangeRate(rate.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.False(await context.ForeignExchangeRates.AnyAsync(x => x.Id == rate.Id));
        }

        /// <summary>
        /// Verifies that attempting to delete a non-existent rate returns NotFound.
        /// </summary>
        [Fact]
        public async Task DeleteForeignExchangeRate_ReturnsNotFound_WhenMissing()
        {
            using var context = CreateInMemoryContext(nameof(DeleteForeignExchangeRate_ReturnsNotFound_WhenMissing));
            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var result = await controller.DeleteForeignExchangeRate(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
