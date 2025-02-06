using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request.OrderRequest;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.Models.ValidatorRequest.OrderValidator
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderValidator(ISupermarketDbContext pContext, int? pCustomerId)
        {
        }
    }
}
