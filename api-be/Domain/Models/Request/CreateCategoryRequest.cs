using api_be.Core.Domain.Interfaces;

namespace api_be.Domain.Models.Request
{
    public record CreateCategoryRequest:IBaseCategory
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Icon { get; set; }

        public int? ParentId { get; set; }
    }
}
