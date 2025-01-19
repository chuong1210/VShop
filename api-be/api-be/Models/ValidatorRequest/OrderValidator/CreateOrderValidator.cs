using api_be.Domain.Interfaces;
using api_be.Models.Request.OrderRequest;

namespace api_be.Models.ValidatorRequest.OrderValidator
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderValidator(ISupermarketDbContext pContext, int? pCustomerId)
        {
        }
    }
}
