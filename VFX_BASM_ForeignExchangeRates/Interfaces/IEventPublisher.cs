using VFX_BASM_ForeignExchangeRates.Models;

namespace VFX_BASM_ForeignExchangeRates.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync(ForeignExchangeRate rate);
    }
}
