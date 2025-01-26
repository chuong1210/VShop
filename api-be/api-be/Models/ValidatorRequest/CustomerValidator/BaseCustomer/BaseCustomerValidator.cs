using api_be.Domain.Interfaces;
using api_be.Transforms;
using api_be.Validators;

namespace api_be.Models.ValidatorRequest.CustomerValidator.BaseCustomer
{
    public class BaseCustomerValidator : AbstractValidator<IBaseCustomer>
    {
        public BaseCustomerValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Name))
                .MaximumLength(Modules.NameMax).WithMessage(ValidatorTransform.MaximumLength(Modules.Name, Modules.NameMax));

            RuleFor(x => x.Phone)
                .Must(phone => phone.Length == Modules.PhoneNumberLength)
                .WithMessage(ValidatorTransform.Length(Modules.PhoneNumber, Modules.PhoneNumberLength))
                .MustAsync(async (phone, token) =>
                {
                    bool exists = true;

                    if (pCurrentId == null)
                    {
                        exists = await pContext.Customers
                                    .AnyAsync(x => x.Phone == phone &&
                                                   x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Customers
                                    .AnyAsync(x => x.Phone == phone &&
                                                   x.Id != pCurrentId &&
                                                   x.IsDeleted == false);
                    }

                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.PhoneNumber));

            RuleFor(x => x.Email)
                .Must(email => ValidatorCustom.BeValidEmail(email))
                .WithMessage(ValidatorTransform.ValidValue(Modules.Email))
                .MustAsync(async (email, token) =>
                {
                    bool exists;

                    if (pCurrentId == null)
                    {
                        exists = await pContext.Customers
                                .AnyAsync(x => x.Email == email && x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Customers
                                .AnyAsync(x => x.Email == email &&
                                               x.Id != pCurrentId &&
                                               x.IsDeleted == false);
                    }

                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.Email));

            RuleFor(x => x.Address)
                .MaximumLength(Modules.AddressMax)
                .WithMessage(ValidatorTransform.MaximumLength(Modules.Address, Modules.AddressMax));

            RuleFor(x => x.Gender)
                .Must(gender => ValidatorCustom.IsValidGender(gender))
                .WithMessage(ValidatorTransform.Must(Modules.Gender, ValidatorCustom.GetGender()));
        }
    }
}
