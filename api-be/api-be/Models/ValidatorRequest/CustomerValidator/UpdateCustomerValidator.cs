using api_be.Domain.Interfaces;
using api_be.Models.Request.CustomerRequest;
using api_be.Models.ValidatorRequest.CustomerValidator.BaseCoupon;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.ValidatorRequest.CustomerValidator

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
