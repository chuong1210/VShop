using api_be.Core.Entities;
using api_be.Core.Models.Common;
using api_be.Domain.Models.Common;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Twilio.TwiML.Messaging;

namespace api_be.Application.Services.KafkaService
{
    public class ElasticSearchConsumer 
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ElasticsearchClient _elasticsearchClient;
        private readonly ILogger<ElasticSearchConsumer> _logger;
        private readonly string _topic;
        private readonly string _indexName;

        public ElasticSearchConsumer(
            IConfiguration configuration,
            ElasticsearchClient elasticsearchClient,
            ILogger<ElasticSearchConsumer> logger)
        {
            _elasticsearchClient = elasticsearchClient;
            _logger = logger;
            _topic = configuration["Kafka:ProductTopic"];
            _indexName = configuration["Elasticsearch:DefaultIndex"];

            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnablePartitionEof = true
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        public   async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);
            _logger.LogInformation("Kafka consumer started...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Waiting for message...");

                    // Thêm logging vào đây để kiểm tra xem có bị dừng không
                    var consumeResult =  _consumer.Consume(stoppingToken);

                    if (consumeResult == null || consumeResult.IsPartitionEOF)
                    {
                        _logger.LogInformation("No new messages, continuing...");
                        continue; // Continue if no new messages
                    }

                    if (consumeResult.Message != null&& consumeResult.Message.Value!=null)
                    {
                        await ProcessMessageAsync(consumeResult.Message.Value);
                        _consumer.Commit(consumeResult);
                        _logger.LogInformation($"Message received: {consumeResult.Message.Value}");

                    }
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Consume error: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
            }
            finally
            {
                _consumer.Close();
            }
        }

        private async Task ProcessMessageAsync(string messageJson)
        {
            try
            {
                var message = JsonSerializer.Deserialize<KafkaMessage<Product>>(messageJson);

                switch (message.Operation.ToLower())
                {
                    case "added":
                    case "modified":
                        //                    var deleteByQueryResponse = await _elasticsearchClient.DeleteByQueryAsync(new DeleteByQueryRequest(_indexName)
                        //                    {
                        //                        Query = new MatchAllQuery()
                        //                    });

                        //                    var bulkRequest = new BulkRequest(_indexName)
                        //                    {
                        //                        Operations = new List<IBulkOperation>
                        //{
                        //    new BulkIndexOperation<Product>(message.Data)
                        //    {
                        //        Id = message.Data.Id.ToString()
                        //    }
                        //}
                        //                    };
                        //                    var bulkResponse = await _elasticsearchClient.BulkAsync(bulkRequest);
                        //                    await _elasticsearchClient.Indices.RefreshAsync(_indexName);

                        //                    if (!bulkResponse.IsValidResponse)
                        //                    {
                        //                        _logger.LogError($"Failed to index product in bulk: {bulkResponse.DebugInformation}");
                        //                    }
                        await IndexProductAsync(message.Data);
                        break;



                    case "deleted":
                        var deleteResponse = await _elasticsearchClient.DeleteAsync<Product>(message.Data.Id, d => d.Index(_indexName));
                        _logger.LogError($"Success to delete index document: {message.Data.Name}");

                        if (!deleteResponse.IsValidResponse)
                        {
                            _logger.LogError($"Failed to delete document: {deleteResponse.DebugInformation}");
                        }
                        break;

                    default:
                        _logger.LogWarning($"Unknown operation: {message.Operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                throw;
            }
        }


        private async Task IndexProductAsync(Product product)
        {
            var response = await _elasticsearchClient.IndexAsync(product, idx => idx.Index("products").Id(product.Id.ToString()));
            if (!response.IsValidResponse)
            {

                _logger.LogError($"Success to delete index document: {response.Id}");

            }
            else
            {
                _logger.LogError($"Failed to index product in bulk: {response.DebugInformation}");
            }
        }

        private async Task DeleteProductFromElasticSearchAsync(int productId)
        {
            var response = await _elasticsearchClient.DeleteAsync<Product>(productId, d => d.Index("products"));
            if (!response.IsValidResponse)
            {
                _logger.LogError($"Failed to delete document: {response.DebugInformation}");
            }
            else
            {
                _logger.LogError($"Success to delete index document: {response.Id}");
            }
        }

                public async void Start()
        {
           await Task.Run(() => ExecuteAsync(CancellationToken.None));
        }

        public  void Dispose()
        {
            _consumer?.Dispose();
            this.Dispose();
        }
    }
}
