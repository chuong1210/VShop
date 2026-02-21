using api_be.Application.Models.Common;
using api_be.Core.Entities;
using api_be.Core.Models.Common;
using api_be.Infrastructure.DB;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public class ProductKafkaConsumer 
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ElasticsearchClient _elasticsearchClient;
        private readonly ILogger<ProductKafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly string _topic;
        private readonly string _indexName;

        public ProductKafkaConsumer(
            IConfiguration configuration,
            ElasticsearchClient elasticsearchClient,
            ILogger<ProductKafkaConsumer> logger,
            IServiceProvider serviceProvider)
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
            _serviceProvider = serviceProvider;
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
            using (var scope = _serviceProvider.CreateScope())
            {
                var redisCacheService = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();
                try
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
                                await UpdateProductCacheAsync(message.Data, redisCacheService);

                                break;



                            case "deleted":
                                //var deleteResponse = await _elasticsearchClient.DeleteAsync<Product>(message.Data.Id, d => d.Index(_indexName));
                                //_logger.LogError($"Success to delete index document: {message.Data.Name}");

                                //if (!deleteResponse.IsValidResponse)
                                //{
                                //    _logger.LogError($"Failed to delete document: {deleteResponse.DebugInformation}");
                                //}
                                await DeleteProductFromElasticSearchAsync(message.Data.Id);
                                await RemoveProductCacheAsync(message.Data.Id, redisCacheService);
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
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing message: {ex.Message}");
                    throw;
                }
                }
        }


        private async Task IndexProductAsync(Product product)
        {
            try
            {
                // Load category nếu có
                Category? category = null;
                if (product.CategoryId.HasValue)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ISupermarketDbContext>();
                    category = await context.Set<Category>()
                        .FirstOrDefaultAsync(c => c.Id == product.CategoryId.Value);
                }

                // Tạo object với Type và Status là INTEGER (match với Python)
                var productForIndex = new
                {
                    // Product fields
                    id = product.Id,
                    internalCode = product.InternalCode,
                    name = product.Name,
                    images = product.Images?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>(),
                    price = product.Price ?? 0,
                    quantity = product.Quantity ?? 0,
                    describes = product.Describes,
                    feature = product.Feature,
                    specifications = product.Specifications,
                    type = (int?)product.Type,        // ← INTEGER, not string
                    status = (int?)product.Status,    // ← INTEGER, not string
                    selling = product.Selling,
                    parentId = product.ParentId,
                    categoryId = product.CategoryId,

                    // Category nested object
                    category = category != null ? new
                    {
                        id = category.Id,
                        internalCode = category.InternalCode,
                        name = category.Name,
                        icon = category.Icon,
                        parentId = category.ParentId
                    } : null,

                    // Auditable fields
                    createdAt = product.CreatedAt,
                    updatedAt = product.UpdatedAt,
                    createdBy = product.CreatedBy,
                    updatedBy = product.UpdatedBy,
                    isDeleted = product.IsDeleted
                };

                var response = await _elasticsearchClient.IndexAsync(
                    productForIndex,
                    idx => idx.Index("products").Id(product.Id.ToString())
                );

                if (response.IsValidResponse)
                {
                    _logger.LogInformation(
                        $"Successfully indexed product: ID={product.Id}, Name={product.Name}, " +
                        $"Type={(int?)product.Type}, Status={(int?)product.Status}, Category={category?.Name ?? "N/A"}"
                    );
                }
                else
                {
                    _logger.LogError($"Failed to index product: ID={product.Id}, Error={response.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while indexing product ID={product.Id}: {ex.Message}");
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

        private async Task UpdateProductCacheAsync(Product product, IRedisCacheService redisCacheService)
        {
            string detailCacheKey = $"product:detail:{product.Id}";

            // Lấy ProductDto từ Product.  Bạn có thể cần inject IMapper ở đây.
            // var productDto = _mapper.Map<ProductDto>(product);  // Nếu cần thiết

            // Cập nhật hoặc tạo mới cache entry
            // await _redisCacheService.SetAsync(detailCacheKey, productDto, TimeSpan.FromHours(1));

            // For now just invalidate the detail cache
            await redisCacheService.RemoveAsync(detailCacheKey);

            _logger.LogInformation($"Product cache updated/invalidated for product ID {product.Id}");

            //Xóa Listcache nếu cần thiết

        }

        // Xóa cache khi xóa sản phẩm
        private async Task RemoveProductCacheAsync(int productId, IRedisCacheService redisCacheService)
        {
            string detailCacheKey = $"product:detail:{productId}";
            await redisCacheService.RemoveAsync(detailCacheKey);
            _logger.LogInformation($"Product cache removed for product ID {productId}");
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
