using api_be.DB;
using api_be.Domain.Interfaces;
using api_be.Models.Common;
using api_be.Transforms;
using FluentValidation;

namespace api_be.Models.ValidatorRequest.DefaultBase
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
