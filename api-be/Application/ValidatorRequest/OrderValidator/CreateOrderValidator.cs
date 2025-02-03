using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.ValidatorRequest.OrderValidator
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderValidator(ISupermarketDbContext pContext, int? pCustomerId)
        {
        }
    }
}
