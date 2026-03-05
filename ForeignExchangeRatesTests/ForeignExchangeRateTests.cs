using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VFX_BASM_ForeignExchangeRates.Controllers;
using VFX_BASM_ForeignExchangeRates.Data;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;
using Xunit;

namespace ForeignExchangeRatesTests
{
    public class ForeignExchangeRateTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

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

            var newRate = new ForeignExchangeRate { BaseCurrency = "usd", QuoteCurrency = "eur", Bid = 1.01m, Ask = 1.11m };

            var action = await controller.CreateForeignExchangeRate(newRate);

            Assert.IsType<ConflictObjectResult>(action.Result);
        }

        [Fact]
        public async Task CreateForeignExchangeRate_PublishesEvent_OnSuccess()
        {
            using var context = CreateInMemoryContext(nameof(CreateForeignExchangeRate_PublishesEvent_OnSuccess));

            var alphaMock = new Mock<IAlphaVantageService>();
            var eventMock = new Mock<IEventPublisher>();
            eventMock.Setup(x => x.PublishAsync(It.IsAny<ForeignExchangeRate>())).Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<ForeignExchangeRateController>>();

            var controller = new ForeignExchangeRateController(context, alphaMock.Object, eventMock.Object, loggerMock.Object);

            var newRate = new ForeignExchangeRate { BaseCurrency = "usd", QuoteCurrency = "cad", Bid = 1.25m, Ask = 1.27m };

            var action = await controller.CreateForeignExchangeRate(newRate);

            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            var returned = Assert.IsType<ForeignExchangeRate>(created.Value);
            Assert.Equal("USD", returned.BaseCurrency);
            Assert.Equal("CAD", returned.QuoteCurrency);

            eventMock.Verify(x => x.PublishAsync(It.Is<ForeignExchangeRate>(r => r.BaseCurrency == "USD" && r.QuoteCurrency == "CAD")), Times.Once);
        }

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
