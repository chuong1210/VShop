using api_be.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Domain.Models.Common
{
    public record KafkaMessage
    {
        public Product Product { get; set; }
        public string Operation { get; set; }
    }
}
