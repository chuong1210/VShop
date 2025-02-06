using api_be.Core.Entities.Dto;

namespace api_be.Core.Domain.Interfaces
{
    public interface IBaseImportGoods
	{
        public string? ReceivingStaff { get; set; }

        public List<DetailImportGoodDto>? Details { get; set; }
    }
}
