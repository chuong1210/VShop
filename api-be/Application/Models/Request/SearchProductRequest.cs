using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request
{
   public record  SearchProductRequest
    {
        public string? productName { get; set; }
        public string? categoryName { get; set; }
    }
}
