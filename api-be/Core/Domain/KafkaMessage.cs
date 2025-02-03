using api_be.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Core.Models.Common
{
    public record KafkaMessage<T>
    {
        public T Data { get; set; }
        public string Operation { get; set; }
    }
}
