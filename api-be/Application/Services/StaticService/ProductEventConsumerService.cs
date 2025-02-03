using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.StaticService
{
    public class ProductEventConsumerService:IHostedService
    {
        private readonly ElasticSearchConsumer _consumer;

        public ProductEventConsumerService(ElasticSearchConsumer consumer)
        {
            _consumer = consumer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _consumer.ExecuteAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Graceful shutdown logic here
            return Task.CompletedTask;
        }
    }
}
