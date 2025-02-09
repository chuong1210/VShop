
using api_be.Application.Services;
using api_be.Application.Services.Imps;
using api_be.Domain.Transforms;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AmazonS3Controller : ControllerBase
    {
        private readonly IAmazonS3Service _AmazonS3Service;

        public AmazonS3Controller(IAmazonS3Service pAmazonS3Service)
        {
            _AmazonS3Service = pAmazonS3Service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileAsync(string pKey)
        {
            try
            {
                var publicUrl = await _AmazonS3Service.GetFileCidFromS3Async(pKey);

                return Ok(new { url = publicUrl });
            }    
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
 

        [HttpPost("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadFileAsync(string pPath, IFormFile pFile)
        {
            if (pFile == null || pFile.Length == 0)
                return BadRequest("No file uploaded.");

            var publicUrl = await _AmazonS3Service.UploadFileAsync(pPath, pFile);

            if (publicUrl == null)
                return StatusCode(500, "An error occurred while uploading the file.");

            return Ok(new { url = publicUrl });
        }


        [HttpPost("file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            try
            {
                var allowedTypes = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".ppt", ".pptx" };
                var fileType = Path.GetExtension(file.FileName).ToLower();

                if (!allowedTypes.Contains(fileType))
                {
                    return BadRequest("Invalid file type.");
                }

                var url = await _AmazonS3Service.UploadFileAsync(file, "user");

                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }
    }
}
