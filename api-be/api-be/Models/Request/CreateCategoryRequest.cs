using api_be.Domain.Interfaces;

namespace api_be.Models.Request
{
    public class CreateCategoryRequest:IBaseCategory
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Icon { get; set; }

        public int? ParentId { get; set; }
    }
}
