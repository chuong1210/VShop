using api_be.Core.Domain.Interfaces;

namespace api_be.Core.Domain
{
    public abstract class HardDeleteEntity : IHardDeleteEntity
    {
        public int Id { get; set; } = default!;
    }
}
