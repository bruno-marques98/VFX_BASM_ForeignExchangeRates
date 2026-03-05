using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
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
    /// Contains unit tests that verify event publishing behavior when foreign exchange rates are created.
    /// </summary>
    public class PublisherTests
    {
        /// <summary>
        /// Creates an <see cref="ApplicationDbContext"/> configured to use an in-memory database.
        /// </summary>
        /// <param name="dbName">A unique name for the in-memory database instance (useful to isolate tests).</param>
        /// <returns>An instance of <see cref="ApplicationDbContext"/> using the in-memory provider.</returns>
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Verifies that creating a foreign exchange rate results in an event being published.
        /// </summary>
        /// <remarks>
        /// Arrange:
        /// - Set up an in-memory <see cref="ApplicationDbContext"/>.
        /// - Mock dependencies: <see cref="IAlphaVantageService"/>, <see cref="IEventPublisher"/>, and a logger.
        /// - Construct <see cref="ForeignExchangeRateController"/> with the mocks.
        /// Act:
        /// - Call <see cref="ForeignExchangeRateController.CreateForeignExchangeRate(ForeignExchangeRateRequest)"/> with a sample request.
        /// Assert:
        /// - Verify that <see cref="IEventPublisher.PublishAsync"/> was invoked once with a <see cref="ForeignExchangeRate"/>
        ///   matching the provided base and quote currencies.
        /// </remarks>
        [Fact]
        public async Task CreateForeignExchangeRate_ShouldPublishEvent_WhenRateIsCreated()
        {
            // Arrange

            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_ShouldPublishEvent_WhenRateIsCreated));

            var alphaServiceMock = new Mock<IAlphaVantageService>();
            var eventPublisherMock = new Mock<IEventPublisher>();
            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(
                context,
                alphaServiceMock.Object,
                eventPublisherMock.Object,
                loggerMock.Object);

            var request = new ForeignExchangeRateRequest
            {
                BaseCurrency = "EUR",
                QuoteCurrency = "USD",
                Bid = 1.10m,
                Ask = 1.20m
            };

            // Act
            var result = await controller.CreateForeignExchangeRate(request);

            // Assert
            eventPublisherMock.Verify(
                x => x.PublishAsync(It.Is<ForeignExchangeRate>(r =>
                    r.BaseCurrency == "EUR" &&
                    r.QuoteCurrency == "USD")),
                Times.Once);

        }
    }
}
