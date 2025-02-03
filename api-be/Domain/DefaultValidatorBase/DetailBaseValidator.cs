using api_be.Infrastructure.DB;
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Common;
using api_be.Domain.Transforms;
using FluentValidation;

namespace api_be.Domain.DefaultValidatorBase
{
    public record DetailBaseCommand
    {
        public int Id { get; set; }

        public bool IsAllDetail { get; set; }
    }
    public class DetailBaseValidator : AbstractValidator<DetailBaseCommand>
    {
        public DetailBaseValidator(ISupermarketDbContext? pContext)
        {
              RuleFor(x => x.Id)
             .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Id));
        }
    }
}
