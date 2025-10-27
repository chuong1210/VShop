using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Responses;
using api_be.Application.Services;
using api_be.Application.Services.KafkaService;
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.ResultResponses;
using api_be.Domain.Transforms;
using api_be.Middleware;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace UI.WebApi.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IProductElasticsearchService _productElasticSearchService;
        private readonly ProductKafkaConsumer _consumerService;

        private readonly IImageSerivce _imageSerivce;
        private readonly ICurrentUserService _currentUserService;

        private readonly Cloudinary _cloudinary;

        private readonly HttpClient _httpClient;  // Inject or initialize HttpClient (e.g., via IHttpClientFactory for best practices)


        public ProductController(IProductService productService,Cloudinary cloudinary, IImageSerivce imageSerivce,IProductElasticsearchService productElasticsearchService,ProductKafkaConsumer elasticSearchConsumer, IHttpClientFactory clientFactory
            ,ICurrentUserService currentUserService)
        {
            _productService = productService;
            _cloudinary = cloudinary;
            _imageSerivce = imageSerivce;
            _productElasticSearchService= productElasticsearchService;
            _consumerService = elasticSearchConsumer; ;
            _httpClient = clientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");
            _currentUserService = currentUserService;
        }
        [HttpPost("start")]
        public async Task<ActionResult> StartConsumer()
        {
            _consumerService.Start();
            return Ok("Consumer started!");
        }
        /// <summary>
        /// Lấy danh sách sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        //[Permission("product.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _productService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }


        /// <summary>
        /// Tìm kiếm danh sách sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet("search")]
        //[Permission("product.view")]
        public async Task<ActionResult> Search([FromQuery] ListBaseCommand pRequest)
        {
            // Build query string for Flask endpoint
            var queryParams = $"?searchKeyword={Uri.EscapeDataString(pRequest.SearchKeyword ?? "")}&page={pRequest.Page}&pageSize={pRequest.PageSize}";

            // Add headers if needed (e.g., for auth)
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());  // Pass user info
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);  // For admin check

            var response = await _httpClient.GetAsync($"smw-api/product/search{queryParams}");

            if (response.IsSuccessStatusCode)
            {
                var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<List<ProductDto>>>();
                return StatusCode((int)response.StatusCode, paginatedResult);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }


        /// <summary>
        /// Lấy danh sách sản phẩm theo nhóm khuyến mãi
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet("combo-products")]
        //[Permission("product.view")]
        public async Task<ActionResult> GetCombo([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _productService.GetListPromotionComBo(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin sản phẩm theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        //[Permission("product.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _productService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm sản phẩm mới
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// - Images: array url
        /// - Price: > 0
        /// - Describes: ckeditor
        /// - Feature: ckeditor
        /// - Specifications: ckeditor
        /// - CategoryId: id có trong Category
        /// </remarks>
        [HttpPost]
        //[Permission("product.create")]
        public async Task<ActionResult> Post([FromBody] CreateProductRequest pRequest)
        {
            var response = await _productService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        //[Permission("product.update")]
        public async Task<ActionResult> Put([FromBody] UpdateProductRequest pRequest)
        {
            var response = await _productService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhận viên thay đổi trạng thái sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - OrderId: Id của sản phẩm
        /// - Status: Trạng thái thay đổi của đơn hàng
        /// Draft(0), Active(1), Pause(2), OutStock(3), Stop(4)
        /// </remarks>
        [HttpPatch]
        //[Permission("product.change-status")]
        public async Task<ActionResult> Change([FromBody] ChangeStatusProductRequest pRequest)
        {
            var response = await _productService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }


        ///// <summary>
        ///// Xóa sản phẩm 
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - Id: int, required
        ///// </remarks>
        [HttpDelete]
        //[Permission("product.delete")]
        public async Task<ActionResult> Delete([FromQuery] int id)
        {
            try
            {
                await _productService.Delete(id);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (NotFoundException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }

    

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
           
                var responses = await _imageSerivce.uploadImage(file);
                return StatusCode(responses.Code, responses);

      
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetProductImage(int id, [FromQuery] int? index = null)
        {
            try
            {
                var result = await _imageSerivce.GetProductImageAsync(id, index);
                return result;
            }
            catch (KeyNotFoundException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);

            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }





    

        [HttpGet("recommendations/collaborative")]
        public async Task<ActionResult> GetCollaborativeRecommendations([FromQuery] RecommendationCommand pRequest)
        {
            var queryParams = $"?numRecs={pRequest.NumRecs}&page={pRequest.Page}&pageSize={pRequest.PageSize}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.GetAsync($"smw-api/recommendations/collaborative{queryParams}");

            if (response.IsSuccessStatusCode)
            {
                var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<List<ProductDto>>>();
                return StatusCode((int)response.StatusCode, paginatedResult);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }

        // New: Similar Products (GET /smw-api/recommendations/similar/{productId})
        [HttpGet("recommendations/similar/{productId}")]
        public async Task<ActionResult> GetSimilarProducts(int productId, [FromQuery] RecommendationCommand pRequest)
        {
            var queryParams = $"?numRecs={pRequest.NumRecs}&page={pRequest.Page}&pageSize={pRequest.PageSize}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.GetAsync($"smw-api/recommendations/similar/{productId}{queryParams}");

            if (response.IsSuccessStatusCode)
            {
                var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<List<ProductDto>>>();
                return StatusCode((int)response.StatusCode, paginatedResult);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }

        // New: Hybrid Recommendations (GET /smw-api/recommendations/hybrid)
        [HttpGet("recommendations/hybrid")]
        public async Task<ActionResult> GetHybridRecommendations([FromQuery] RecommendationCommand pRequest)
        {
            var queryParams = $"?numRecs={pRequest.NumRecs}&page={pRequest.Page}&pageSize={pRequest.PageSize}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.GetAsync($"smw-api/recommendations/hybrid{queryParams}");

            if (response.IsSuccessStatusCode)
            {
                var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<List<ProductDto>>>();
                return StatusCode((int)response.StatusCode, paginatedResult);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }

        // New: Track Interaction (POST /smw-api/recommendations/track)
        [HttpPost("recommendations/track")]
        public async Task<ActionResult> TrackInteraction([FromBody] TrackInteractionCommand pRequest)
        {
            var json = JsonSerializer.Serialize(pRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.PostAsync("smw-api/recommendations/track", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TrackResult>(); // Define a simple DTO for this
                return StatusCode((int)response.StatusCode, result);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }

        // New: Trending Products (GET /smw-api/recommendations/trending)
        [HttpGet("recommendations/trending")]
        public async Task<ActionResult> GetTrendingProducts([FromQuery] RecommendationCommand pRequest)
        {
            var queryParams = $"?numRecs={pRequest.NumRecs}&page={pRequest.Page}&pageSize={pRequest.PageSize}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.GetAsync($"smw-api/recommendations/trending{queryParams}");

            if (response.IsSuccessStatusCode)
            {
                var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<List<ProductDto>>>();
                return StatusCode((int)response.StatusCode, paginatedResult);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }

        // New: Start Consumer (POST /smw-api/product/start) - Optional, for admin
        [HttpPost("index")]
        //[Permission("product.admin")] // Assuming permission for admin
        public async Task<ActionResult> IndexProducts()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-User-ID", _currentUserService.UserId.ToString());
            _httpClient.DefaultRequestHeaders.Add("X-User-Type", _currentUserService.Type);

            var response = await _httpClient.PostAsync("smw-api/product/index", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IndexResult>(); // Define a simple DTO for this
                return StatusCode((int)response.StatusCode, result);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { Error = error });
            }
        }
    }

    public class RecommendationCommand
    {
        public int NumRecs { get; set; } = 10;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class TrackInteractionCommand
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string Type { get; set; } = "view"; // view, like, purchase
    }

    // Simple DTOs for non-paginated responses
    public class IndexResult
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TrackResult
    {
        public string Message { get; set; } = string.Empty;
    }

    public class SimpleMessageResult
    {
        public string Message { get; set; } = string.Empty;
    
}
}
