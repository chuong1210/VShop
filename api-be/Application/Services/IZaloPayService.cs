using api_be.Application.Models.Request.PaymentRequest;
using api_be.Domain.ResultResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services
{
    public interface IZaloPayService
    {
        public Task<Result<string>> CreateZaloPayPayment(CreateZaloPayRequest request);


    }

}
