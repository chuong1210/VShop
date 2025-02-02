using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using System.Text;
using api_be.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using api_be.Core.Domain;
using Confluent.Kafka.Admin;

namespace api_be.Infrastructure.DB.Interceptors
{
  
    public class KafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly IConfiguration _configuration;
        public async void CreateTopic(string topicName)
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };

            using (var adminClient = new AdminClientBuilder(config).Build())
            {
                try
                {
                    var metadata =  adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                    var topicExists = metadata.Topics.Exists(t => t.Topic == topicName);

                    if (!topicExists)
                    {
                        var topicSpecification = new TopicSpecification
                        {
                            Name = topicName,
                            NumPartitions = 1,
                            ReplicationFactor = 1
                        };

                        adminClient.CreateTopicsAsync(new[] { topicSpecification }).Wait();
                        Console.WriteLine($"Topic '{topicName}' has been created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Topic {topicName} already exists.");
                    }
                }
                catch (CreateTopicsException e)
                {
                    Console.WriteLine($"An error occurred while creating the topic: {e.Error.Reason}");
                }
            }
        }
    
    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
        {
            _configuration = configuration;

            var config = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };


            _topic = _configuration["Kafka:ProductTopic"];
            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;
        }

        public async Task SendMessageAsync<T>(T data, string operation) where T:AuditableEntity
        {
            //var message = new
            //{
            //    Id = product.Id,
            //    Name = product.Name,
            //    Price = product.Price,
            //    UpdatedAt = product.UpdatedAt,
            //    Operation = operation // INSERT, UPDATE, DELETE
            //};

            var message = new
            {
                Data = data,
                Operation = operation // INSERT, UPDATE, DELETE
            };
            string jsonMessage = JsonConvert.SerializeObject(message);

            await _producer.ProduceAsync(_topic, new Message<string, string>
            {
                Key = data.Id.ToString(),
                Value = jsonMessage
            });

            _logger.LogInformation($"Sent Kafka Message: {jsonMessage}");
        }
    }

}
