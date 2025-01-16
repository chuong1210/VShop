using api_be.Domain.Interfaces;
using api_be.Models.Common;

namespace api_be.Models.Request
{
    public record UpdateCategoryRequest : BaseDto,IBaseCategory
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Icon { get; set; }

        public int? ParentId { get; set; }
    }
}
