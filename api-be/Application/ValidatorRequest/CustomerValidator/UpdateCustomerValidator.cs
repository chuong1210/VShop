using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.CustomerRequest;
using api_be.Application.ValidatorRequest.CustomerValidator.BaseCustomer;
using api_be.Domain.DefaultValidatorBase;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.ValidatorRequest.CustomerValidator

{
    public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
    {
        public UpdateCustomerValidator(ISupermarketDbContext pContext, int? pCurrentId)
        {
            Include(new UpdateBaseValidator<UpdateCustomerRequest>(pContext));
            Include(new BaseCustomerValidator(pContext, pCurrentId));
        }
    
    }
}
