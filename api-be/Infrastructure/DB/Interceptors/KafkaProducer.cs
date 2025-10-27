using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using api_be.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using api_be.Core.Domain;
using Confluent.Kafka.Admin;
using System.Text.Json;
using api_be.Core.Models.Common;

namespace api_be.Infrastructure.DB.Interceptors
{

    public class KafkaProducer<TKey, TValue>:IDisposable
    {
        private readonly string _topic;
        private readonly IProducer<TKey, string> _producer;
        private readonly ILogger<KafkaProducer<TKey, TValue>> _logger;
        private readonly IConfiguration _configuration;
        private static bool _isConsumerRunning = false;

        private async Task StartConsumerIfNotRunning()
        {
            if (!_isConsumerRunning)
            {
                using var httpClient = new HttpClient();
                await httpClient.PostAsync("https://localhost:7288/smw-api/product/start", null);
                _isConsumerRunning = true;

            }
        }
        public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer<TKey, TValue>> logger)
        {
            _configuration = configuration;

          
            _topic = _configuration["Kafka:ProductTopic"] ?? "product-changes";
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                AllowAutoCreateTopics = false,
                Acks = Acks.All,
                RetryBackoffMs = 500, // Add retry backoff
                MessageSendMaxRetries = 3 // Add retries

            };

            // In KafkaProducer constructor, add this after building _producer
            CreateTopic(_topic);
            _producer = new ProducerBuilder<TKey, string>(config).Build();
            // Gửi một message test để kích hoạt Consumer
            //var testMessage = new KafkaMessage<Product>
            //{
            //    Operation = "modified",
            //    Data = new Product { Id = 25, Name = "Test Product", Price = 0 }
            //};

            //_producer.ProduceAsync(_topic, new Message<TKey, string>
            //{
            //    Key = (TKey)(object)testMessage.Data.Id.ToString(),
            //    Value = JsonSerializer.Serialize(testMessage)
            //});
        }

        public async Task ProduceAsync(TKey key, TValue value)
        {
            try
            {
                //await StartConsumerIfNotRunning(); // Chạy consumer trước khi gửi tin nhắn

                var message = new Message<TKey, string>
                {
                    Key = key,
                    Value = JsonSerializer.Serialize<TValue>(value)
                };

                var result = await _producer.ProduceAsync(_topic, message);

                _logger.LogInformation($"Message delivered to '{result.TopicPartitionOffset}'");
            }
            catch (ProduceException<TKey, string> ex)
            {
                _logger.LogError($"Failed to deliver message: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _producer.Dispose();
        }
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
                    var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
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
                        _logger.LogInformation($"Topic '{topicName}' has been created successfully.");
                    }
                    else
                    {
                        _logger.LogError($"Topic {topicName} already exists.");
                    }
                }
                catch (CreateTopicsException e)
                {
                    _logger.LogError($"An error occurred while creating the topic: {e.Error.Reason}");
                }
            }
        }


        public   IProducer<TKey,String>  InitializeAsync()
        {
            return _producer;
         
        }
        //public async Task SendMessageAsync<T>(T data, string operation) where T : AuditableEntity
        //{
        //    try
        //    {
        //        //var message = new
        //        //{
        //        //    Id = product.Id,
        //        //    Name = product.Name,
        //        //    Price = product.Price,
        //        //    UpdatedAt = product.UpdatedAt,
        //        //    Operation = operation // INSERT, UPDATE, DELETE
        //        //};

        //        var message = new
        //        {
        //            Data = data,
        //            Operation = operation // INSERT, UPDATE, DELETE
        //        };
        //        string jsonMessage = JsonConvert.SerializeObject(message);

        //     var deliveryResult=   await _producer.ProduceAsync(_topic, new Message<string, string>
        //        {
        //            Key = data.Id.ToString(),
        //            Value = jsonMessage
        //        });

        //        _logger.LogInformation($"Sent Kafka Message: {jsonMessage} , Offset value: {deliveryResult.Offset}");
        //    }
        //    catch(ProduceException<String,String> e)
        //    {
        //        _logger.LogError($"Delivery failed: {e.Error.Reason}"); 
        //    }
        //}
    }
}
