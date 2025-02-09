using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Elastic.Clients.Elasticsearch.Requests;
using api_be.Application.Models.Request.PaymentRequest;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;
using api_be.Domain.ResultResponses;
using Microsoft.AspNetCore.Http;
using api_be.Infrastructure.DB;
using AutoMapper;
using api_be.Core.Entities;
using api_be.Domain.Extensions;
using api_be.Application.Models.Common;
using Microsoft.Extensions.Options;
using api_be.Application.Responses.PaymentResponse;

namespace api_be.Application.Services.PaymentService
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class ZaloPayService:IZaloPayService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private ZaloPayConfig zaloPayConfig;

        public ZaloPayService(IConfiguration configuration, HttpClient httpClient,ISupermarketDbContext context,IMapper mapper, IOptions<ZaloPayConfig> config)
        {
            zaloPayConfig = config?.Value ?? throw new ArgumentNullException(nameof(config));

            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            _mapper = mapper;
        }

        private string GenerateHmacSHA256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        private async Task<Result<PaymentLinkDto>> GetLink(CreatePaymentUrlRequest request)
        {
            try
            { 
            var paymentUrl = string.Empty;

            var zalopayPayRequest = new CreateZaloPayRequest(zaloPayConfig.AppId, zaloPayConfig.AppUser,
                             DateTime.Now.GetTimeStamp(), (long)request.RequiredAmount!, DateTime.Now.ToString("yymmdd") + "_" + request.PaymentRefId ?? string.Empty,
                             "zalopayapp", request.PaymentContent ?? string.Empty);
            zalopayPayRequest.MakeSignature(zaloPayConfig.Key1);
            (bool createZaloPayLinkResult, string? createZaloPayMessage) = zalopayPayRequest.GetLink(zaloPayConfig.PaymentUrl);
            if (createZaloPayLinkResult)
            {
                paymentUrl = createZaloPayMessage;
            }
            else
            {
                var Messages = new List<String> { createZaloPayMessage };
            }
                var paymentLinkDto = new PaymentLinkDto()
                {
                    PaymentId = request.PaymentRefId.ToString() ?? string.Empty,
                    PaymentUrl = paymentUrl,
                };
                return Result<PaymentLinkDto>.Success(paymentLinkDto, StatusCodes.Status201Created);
        }
            catch (Exception ex)
            {
                return Result<PaymentLinkDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

}
        private async Task<string> CreateOrderAsync(CreateZaloPayRequest request)
        {
            var appId = _configuration["ZaloPay:AppId"];
            var key1 = _configuration["ZaloPay:Key1"];
            var endpoint = _configuration["ZaloPay:Endpoint"];
            var callbackUrl = _configuration["ZaloPay:CallbackUrl"];

            var order = new
            {
                app_id = appId,
                app_trans_id = $"{DateTime.UtcNow:yyMMdd}_{request.OrderId}",
                app_user = "demo_user",
                app_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                amount = request.Amount,
                description = request.Description,
                embed_data = "{}",
                item = "[]",
                bank_code = "",
                callback_url = callbackUrl
            };

            var rawData = $"{appId}|{order.app_trans_id}|{order.app_user}|{order.amount}|{order.app_time}|{order.embed_data}|{order.item}";
            var mac = GenerateHmacSHA256(rawData, key1);

            var requestData = new
            {
                order.app_id,
                order.app_trans_id,
                order.app_user,
                order.app_time,
                order.amount,
                order.description,
                order.embed_data,
                order.item,
                order.bank_code,
                order.callback_url,
                mac
            };

            var jsonRequest = JsonConvert.SerializeObject(requestData);
            var response = await _httpClient.PostAsync(endpoint, new StringContent(jsonRequest, Encoding.UTF8, "application/json"));

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

            return jsonResponse?.order_url;
        }


        public async Task<Result<string>> CreateZaloPayPayment(CreateZaloPayRequest request)
        {
            try
            {
                var payment = _mapper.Map<Payment>(request);
                await _context.Set<Payment>().AddAsync(payment);
                await _context.SaveChangesAsync();
                request.Description = $"Thanh toán đơn hàng {payment.Id}";
                string orderUrl = await CreateOrderAsync(
                request);

                return Result<string>.Success(orderUrl, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Lỗi khi tạo đơn hàng: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }

}
