using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services
{
    public interface IAmazonS3Service
    {
        Task<string> UploadFileAsync(string pPath, IFormFile pFile);
        Task<string> UploadFileAsync(IFormFile file, string folder);

        Task<string> GetFileCidFromS3Async(string key);
        //Task<byte[]> DownloadFileAsync(string key);

    }
}
