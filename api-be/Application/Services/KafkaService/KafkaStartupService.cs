using api_be.Infrastructure.DB.Interceptors;
using Confluent.Kafka;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.KafkaService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using api_be.Infrastructure.DB.Interceptors;
    using api_be.Core.Entities;
    using api_be.Core.Models.Common;

    public class KafkaStartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaStartupService> _logger;
        public KafkaStartupService(IServiceProvider serviceProvider, ILogger<KafkaStartupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var producer = scope.ServiceProvider.GetRequiredService<KafkaProducer<string, KafkaMessage<Product>>>();

                // Gửi một message test vào Kafka
                var testMessage = new KafkaMessage<Product>
                {
                    Operation = "test",
                    Data = new Product
                    {
                        Id = -1,
                        Name = "Test Product",
                        Price = 0,
                        Quantity = 0,
                        Status = Product.ProductStatus.Active,
                    }
                };

                await producer.ProduceAsync(testMessage.Data.Id.ToString(), testMessage);

                _logger.LogInformation("✅ Sent initial test message to Kafka.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
