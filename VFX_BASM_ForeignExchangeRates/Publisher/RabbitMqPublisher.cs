using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VFX_BASM_ForeignExchangeRates.Events;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Publisher
{
    public class RabbitMqPublisher : IEventPublisher
    {
        public async Task PublishAsync(ForeignExchangeRate rate)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "fxrate.created",
                durable: false,
                exclusive: false,
                autoDelete: false);

            var evt = new ForeignExchangeCreatedEvent(rate.BaseCurrency, rate.QuoteCurrency,
                rate.Bid, rate.Ask, rate.Timestamp);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "fxrate.created",
            body: body);
        }
    }
}
