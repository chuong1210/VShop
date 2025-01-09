using api_be.Domain.Interfaces;

namespace api_be.Domain.Common
{
    public abstract class HardDeleteEntity : IHardDeleteEntity
    {
        public int Id { get; set; } = default!;
    }
}
