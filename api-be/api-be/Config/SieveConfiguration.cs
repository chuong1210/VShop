using api_be.Domain.Entities;
using Sieve.Services;

namespace api_be.Config
{
    public class SieveConfiguration : ISieveConfiguration
    {
        public void Configure(SievePropertyMapper mapper)
        {
            mapper.Property<Product>(p => p.Name)
                .CanFilter()
                .CanSort();

            mapper.Property<Product>(p => p.Price)
                .CanFilter()
                .CanSort();
        }
    }
}
