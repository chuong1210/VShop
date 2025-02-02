using api_be.Core.Entities;
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

namespace api_be.Application.Services.StaticService
{
    public class KafkaConsumerService: BackgroundService
    {
        private readonly ElasticsearchClient _elasticsearchClient;
        private readonly IConsumer<string, string> _consumer;
        private readonly IConfiguration _configuration;

        public KafkaConsumerService(ElasticsearchClient elasticsearchClient,IConfiguration configuration)
        {
            _configuration= configuration;
            _elasticsearchClient = elasticsearchClient;
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"], // Read from appsettings.json
                GroupId = _configuration["Kafka:GroupId"], // Read from appsettings.json
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_configuration["Kafka:ProductTopic"]);

            // Set a maximum wait time before checking for no message
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), timeoutCancellationTokenSource.Token); // 30 seconds timeout
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //var consumeResult = _consumer.Consume(stoppingToken);
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5)); // Timeout sau 5 giây

                    if (consumeResult != null)
                    {
                        var message = JsonSerializer.Deserialize<KafkaMessage>(consumeResult.Message.Value);
                        string action = message.Operation;

                        if (action == "Added" || action == "Modified")
                        {
                            await _elasticsearchClient.IndexAsync(message.Product, i => i.Index("products"));
                        }
                        else if (action == "Deleted")
                        {
                            await _elasticsearchClient.DeleteAsync<object>(message.Product.Id, d => d.Index("products"));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No message received within timeout.");
                    }
                    if (timeoutTask.IsCompleted)
                    {
                        Console.WriteLine("Timeout reached without receiving any messages. Exiting...");
                        break;
                    }
                }
                catch (ConsumeException e)
                {
                    Console.Write($"Error while consuming message: {e.Error.Reason}");
                }
            }
        }

    }
}
