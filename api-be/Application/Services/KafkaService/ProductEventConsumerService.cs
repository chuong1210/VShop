using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.KafkaService
{
    public class ProductEventConsumerService : IHostedService
    {
        private readonly ElasticSearchConsumer _consumer;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public ProductEventConsumerService(ElasticSearchConsumer consumer, IHostApplicationLifetime applicationLifetime)
        {
            _consumer = consumer;
            _applicationLifetime = applicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Đảm bảo consumer chỉ bắt đầu khi ứng dụng đã được khởi động.
            _applicationLifetime.ApplicationStarted.Register(() =>
            {
                // Khởi động consumer sau khi ứng dụng đã sẵn sàng.
                Task.Run(() => _consumer.ExecuteAsync(cancellationToken));
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Graceful shutdown logic here
            _consumer.Dispose(); // Đảm bảo consumer đóng đúng cách khi ứng dụng tắt

            return Task.CompletedTask;
        }
    }
}
