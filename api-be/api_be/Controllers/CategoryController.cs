using api_be.Domain.Exceptions;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.Services;
using api_be.Domain.Transforms;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // 1. Create Category
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            var result = await _categoryService.Create(request);
            if (result.Succeeded)
            {
                return StatusCode(result.Code, result.Data); // Return created category
            }
            return BadRequest(result.Messages); // Return validation errors if failure
        }

        // 2. Delete Category
        //[HttpDelete("{id}")]
        [HttpDelete]

        public async Task<IActionResult> Delete(int id)
        {
            try

            {
                var result = await _categoryService.Delete(id);

                return StatusCode(StatusCodes.Status204NoContent, result.Data); // Return success
            }
            catch (NotFoundException ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }

        // 3. Get Category Details
        //[HttpGet("detail/{id}")]
        [HttpGet("detail")]

        public async Task<IActionResult> GetDetail([FromQuery] int Id, [FromQuery] bool isAllDetal)//[FromRoute] int id
        {
            var request = new DetailBaseCommand { Id = Id , IsAllDetail=isAllDetal};

            var result = await _categoryService.Detail(request);
            if (result.Succeeded)
            {
                return StatusCode(result.Code, result);
            }
            throw new NotFoundException(Modules.Category.Module, Id.ToString());
        }

        // 4. Get List of Categories (with Pagination)
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] ListBaseCommand request)
        {
            var response = await _categoryService.GetList(request);
            if (response.Succeeded)
            {
                return StatusCode(response.Code, response);
            }
            return StatusCode(response.Code, response);
        }

        // 5. Update Category
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            request.Id = id; // Assign id to request body
            var result = await _categoryService.Update(request);
            if (result.Succeeded)
            {
                return StatusCode(result.Code, result);
            }
            return BadRequest(result.Messages); // Return failure message
        }
    }

}

