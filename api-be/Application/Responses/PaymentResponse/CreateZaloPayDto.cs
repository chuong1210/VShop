using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Responses.PaymentResponse
{
    public record CreateZaloPayDto
    {
        public int returnCode { get; set; }
        public string returnMessage { get; set; } = string.Empty;
        public string orderUrl { get; set; } = string.Empty;
        public string zpTransToken { get; set; } = string.Empty;
    }
}
