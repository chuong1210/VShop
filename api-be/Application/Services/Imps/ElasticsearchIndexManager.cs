using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Logging;

namespace api_be.Application.Services.Imps
{
    public class ProductIndexInitializer
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<ProductIndexInitializer> _logger;
        private const string INDEX_NAME = "products";

        public ProductIndexInitializer(
            ElasticsearchClient elasticClient,
            ILogger<ProductIndexInitializer> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var existsResponse = await _elasticClient.Indices.ExistsAsync(INDEX_NAME);

                if (existsResponse.Exists)
                {
                    _logger.LogWarning($"Index '{INDEX_NAME}' already exists. Deleting...");

                    var deleteResponse = await _elasticClient.Indices.DeleteAsync(INDEX_NAME);

                    if (deleteResponse.IsValidResponse)
                    {
                        _logger.LogInformation($"Successfully deleted old index '{INDEX_NAME}'");
                    }
                    else
                    {
                        _logger.LogError($"Failed to delete index: {deleteResponse.DebugInformation}");
                        return;
                    }
                }

                // Tạo index mới với mapping đúng
                var createResponse = await _elasticClient.Indices.CreateAsync(INDEX_NAME, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(1)
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Standard("standard_analyzer")
                            )
                        )
                    )
                    .Mappings(m => m
                        .Properties<ProductIndexModel>(p => p
                            // Product fields
                            .IntegerNumber(n => n.Id)
                            .Keyword(k => k.InternalCode)
                            .Text(t => t.Name, td => td
                                .Fields(f => f.Keyword("keyword"))
                            )
                            .Keyword(k => k.Images)
                            .ScaledFloatNumber(sf => sf.Price, sfd => sfd.ScalingFactor(100))
                            .IntegerNumber(n => n.Quantity)
                            .Text(t => t.Describes)
                            .Text(t => t.Feature)
                            .Text(t => t.Specifications)
                            .Keyword(k => k.Type)
                            .Keyword(k => k.Status)
                            .IntegerNumber(n => n.Selling)
                            .IntegerNumber(n => n.ParentId)
                            .IntegerNumber(n => n.CategoryId)

                            // Nested Category object
                            //.Object<CategoryIndexModel>(o => o.Category, od => od
                            //    .Properties(cp => cp
                            //        .IntegerNumber(n => n.Id)
                            //        .Keyword(k => k.InternalCode)
                            //        .Text(t => t.Name, td => td
                            //            .Fields(f => f.Keyword("keyword"))
                            //        )
                            //        .Keyword(k => k.Icon)
                            //        .IntegerNumber(n => n.ParentId)
                            //    )
                            //)

                            // Auditable fields
                            .Date(d => d.CreatedAt)
                            .Date(d => d.UpdatedAt)
                            .Keyword(k => k.CreatedBy)
                            .Keyword(k => k.UpdatedBy)
                            .Boolean(b => b.IsDeleted)
                        )
                    )
                );

                if (createResponse.IsValidResponse)
                {
                    _logger.LogInformation($"Successfully created index '{INDEX_NAME}' with correct mapping");
                }
                else
                {
                    _logger.LogError($"Failed to create index: {createResponse.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing Elasticsearch index '{INDEX_NAME}'");
            }
        }
    }

    // Models for Elasticsearch indexing
    public class ProductIndexModel
    {
        public int Id { get; set; }
        public string? InternalCode { get; set; }
        public string? Name { get; set; }
        public string? Images { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Describes { get; set; }
        public string? Feature { get; set; }
        public string? Specifications { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public int? Selling { get; set; }
        public int? ParentId { get; set; }
        public int? CategoryId { get; set; }
        public CategoryIndexModel? Category { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public class CategoryIndexModel
    {
        public int Id { get; set; }
        public string? InternalCode { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public int? ParentId { get; set; }
    }
}