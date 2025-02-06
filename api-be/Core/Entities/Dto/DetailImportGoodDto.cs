using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Core.Entities.Dto
{
    public record DetailImportGoodDto
    {
        public int? ImportQuantity { get; set; }

        public int? ProductId { get; set; }
    }
}
