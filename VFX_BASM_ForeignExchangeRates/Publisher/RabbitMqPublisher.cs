using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VFX_BASM_ForeignExchangeRates.Events;
using VFX_BASM_ForeignExchangeRates.Interfaces;
using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Publisher
{
    /// <summary>
    /// Publishes foreign exchange rate events to a RabbitMQ broker.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// - Connects to a RabbitMQ broker at <c>localhost</c>.
    /// - Declares a queue named <c>fxrate.created</c> (non-durable, non-exclusive, not auto-deleted).
    /// - Creates a <see cref="ForeignExchangeCreatedEvent"/> from the provided <see cref="ForeignExchangeRate"/>,
    ///   serializes it to JSON and publishes it to the declared queue using the empty exchange and routing key
    ///   <c>fxrate.created</c>.
    /// Connection and channel resources are disposed asynchronously via <c>await using</c>.
    /// </remarks>
    /// <seealso cref="IEventPublisher"/>
    /// <seealso cref="ForeignExchangeCreatedEvent"/>
    public class RabbitMqPublisher : IEventPublisher
    {
        /// <summary>
        /// Publishes the specified <see cref="ForeignExchangeRate"/> to the RabbitMQ queue as a
        /// <see cref="ForeignExchangeCreatedEvent"/>.
        /// </summary>
        /// <param name="rate">The foreign exchange rate to publish. This is transformed into a <see cref="ForeignExchangeCreatedEvent"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been published.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="rate"/> is <c>null</c>. (Not validated in current implementation.)</exception>
        /// <remarks>
        /// The method performs the following steps:
        /// 1. Creates a <see cref="ConnectionFactory"/> targeting <c>localhost</c>.
        /// 2. Opens an asynchronous connection and channel.
        /// 3. Declares the queue <c>fxrate.created</c> with the following flags:
        ///    - durable: false
        ///    - exclusive: false
        ///    - autoDelete: false
        /// 4. Maps the provided <paramref name="rate"/> to <see cref="ForeignExchangeCreatedEvent"/>, serializes it to UTF-8 JSON,
        ///    and publishes the bytes using <c>BasicPublishAsync</c> with an empty exchange and routing key <c>fxrate.created</c>.
        /// 
        /// Note: Exceptions from the RabbitMQ client (connection failures, publish errors) will propagate to the caller.
        /// </remarks>
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
