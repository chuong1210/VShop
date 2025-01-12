

using api_be.Common.Interfaces;
using api_be.Extensions;
using api_be.Interfaces;
using api_be.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Validator.BaseUser
{
    public class BaseUserValidator : AbstractValidator<IBaseUser>
    {
        public BaseUserValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            //RuleFor(x => new { x.Email, x.PhoneNumber })
            //    .Must(x => !string.IsNullOrEmpty(x.Email) || !string.IsNullOrEmpty(x.PhoneNumber))
            //    .WithMessage("Either Email or PhoneNumber must be provided.");

            RuleFor(x => x.Email)
                .Must(email => /*string.IsNullOrEmpty(email) ||*/ ValidatorExtension.BeValidEmail(email))
                .WithMessage(ValidatorTransform.ValidValue(Modules.Email))
                .MustAsync(async (email, token) =>
                {
                    bool exists;

                    if(pCurrentId == null)
                    {
                        exists = await pContext.Users
                                .AnyAsync(x => x.Email == email &&
                                               x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Users
                                .AnyAsync(x => x.Email == email &&
                                               x.Id != pCurrentId &&
                                               x.IsDeleted == false);
                    }

                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.Email));

            RuleFor(x => x.PhoneNumber)
                .Must(phoneNumber => phoneNumber.Length == Modules.PhoneNumberLength)
                .WithMessage(ValidatorTransform.Length(Modules.PhoneNumber, Modules.PhoneNumberLength))
                .MustAsync(async (phone, token) =>
                {
                    bool exists = true;

                    if (pCurrentId == null)
                    {
                        exists = await pContext.Users
                                .AnyAsync(x => x.PhoneNumber == phone &&
                                               x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Users
                                .AnyAsync(x => x.PhoneNumber == phone &&
                                               x.Id != pCurrentId &&
                                               x.IsDeleted == false);
                    }

                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.PhoneNumber));
        }
    }
}
