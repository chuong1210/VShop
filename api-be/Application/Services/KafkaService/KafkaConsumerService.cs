using api_be.Core.Entities;
using api_be.Core.Models.Common;
using api_be.Domain.Models.Common;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace api_be.Application.Services.KafkaService
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ElasticsearchClient _elasticsearchClient;
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IConfiguration _configuration;

        public KafkaConsumerService(ElasticsearchClient elasticsearchClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _elasticsearchClient = elasticsearchClient;
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"], // Read from appsettings.json
                GroupId = _configuration["Kafka:GroupId"], // Read from appsettings.json
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SecurityProtocol = SecurityProtocol.Plaintext // Hoặc SecurityProtocol.Ssl nếu dùng SSL
            };
            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _consumer.Subscribe(_configuration["Kafka:ProductTopic"]);

            // Set a maximum wait time before checking for no message
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), timeoutCancellationTokenSource.Token); // 30 seconds timeout
            while (!stoppingToken.IsCancellationRequested)
            {

                //var consumeResult = _consumer.Consume(stoppingToken);
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5)); // Timeout sau 5 giây
                if (consumeResult == null)
                    continue;
                if (consumeResult != null)
                {
                    var message = JsonSerializer.Deserialize<KafkaMessage<Product>>(consumeResult.Message.Value);
                    string action = message.Operation;

                    if (action == "Added" || action == "Modified")
                    {
                        await _elasticsearchClient.IndexAsync(message.Data, i => i.Index("products"));
                    }
                    else if (action == "Deleted")
                    {
                        await _elasticsearchClient.DeleteAsync<object>(message.Data.Id, d => d.Index("products"));
                    }
                }

                //if (timeoutTask.IsCompleted)
                //{
                //    Console.WriteLine("Timeout reached without receiving any messages. Exiting...");
                //    break;
                //}


            }
            _consumer.Close();




        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer.Close();
            return Task.CompletedTask;
        }
    }
}


//using api_be.Core.Entities;
//using api_be.Domain.Models.Common;
//using Confluent.Kafka;
//using Elastic.Clients.Elasticsearch;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace api_be.Application.Services.KafkaService
//{
//    public class KafkaConsumerService : BackgroundService
//    {
//        private readonly IConsumer<Ignore, string> _consumer;

//        private readonly ILogger<KafkaConsumerService> _logger;

//        public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger)
//        {
//            _logger = logger;

//            var consumerConfig = new ConsumerConfig
//            {
//                BootstrapServers = configuration["Kafka:BootstrapServers"],
//                GroupId = configuration["Kafka:GroupId"],
//                AutoOffsetReset = AutoOffsetReset.Earliest
//            };

//            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _consumer.Subscribe("product-changes");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                ProcessKafkaMessage(stoppingToken);

//                Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
//            }

//            _consumer.Close();
//        }

//        public void ProcessKafkaMessage(CancellationToken stoppingToken)
//        {
//            try
//            {
//                var consumeResult = _consumer.Consume(stoppingToken);

//                var message = consumeResult.Message.Value;

//                _logger.LogInformation($"Received inventory update: {message}");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"Error processing Kafka message: {ex.Message}");
//            }
//        }

//    }
//}
