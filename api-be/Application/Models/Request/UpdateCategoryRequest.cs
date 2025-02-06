using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Common;

namespace api_be.Application.Models.Request
{
    public record UpdateCategoryRequest : BaseDto,IBaseCategory
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Icon { get; set; }

        public int? ParentId { get; set; }
    }
}
