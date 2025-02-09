using Amazon.S3;
using Amazon.S3.Model;
using api_be.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class AmazonS3Service : IAmazonS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private const string BUCKET_NAME = "v-shop";
        private const string accessKey = "12D2E177B2034A9BC155";
        private const string secretKey = "23hR9mSYM59pmQEWaIFAavObD582pAaS7JtNzMR0";
        private const string FILEBASE_API_TOKEN = "MTJEMkUxNzdCMjAzNEE5QkMxNTU6MjNoUjltU1lNNTlwbVFFV2FJRkFhdk9iRDU4MnBBYVM3SnROek1SMDp2LXNob3A=";  // 🔥 Thay thế bằng API Token của bạn

        public AmazonS3Service()
        {
            var config = new AmazonS3Config
            {
                ServiceURL = "https://s3.filebase.com",
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1" // 👈 Thêm dòng này nếu cần

            };
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task<string> UploadFileAsync(string pPath, IFormFile pFile)
        {
            try
            {
                var key = $"Uploads/{pPath}/{pFile.FileName}";

                // Tải file lên S3
                await UploadObjectFromFileAsync(pFile, BUCKET_NAME, key);

                string url = await GetFileCidFromS3Async(key);

                return url;
            }
            catch (AmazonS3Exception ex)
            {
                // Xử lý lỗi và ném ra ngoại lệ
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
        }

        private string GetContentType(string fileName)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Mặc định nếu không tìm thấy
            }
            return contentType;
        }


        private async Task UploadObjectFromFileAsync(IFormFile file, string bucketName, string key)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = GetContentType(file.FileName), // 👈 Đặt ContentType đúng
                    };

                    putRequest.Metadata.Add("x-amz-meta-title", file.FileName);

                    // ✅ Thêm các metadata cần thiết
                    if (file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                    {
                        putRequest.Metadata.Add("Content-Disposition", "attachment");
                    }

                    // Upload lên S3
                    await _s3Client.PutObjectAsync(putRequest);
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var fileName = $"{folder}/{Guid.NewGuid()}_{file.FileName}";

            using (var stream = file.OpenReadStream())
            {
                var request = new PutObjectRequest
                {
                    BucketName = BUCKET_NAME,
                    Key = "Uploads"+fileName,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead, // Cho phép truy cập công khai,
                        AutoCloseStream = true

                };

                await _s3Client.PutObjectAsync(request);
            }

            return $"https://s3.filebase.com/{BUCKET_NAME}/{fileName}";
        }
        public async Task<string> GetFileCidFromS3Async(string key)
        {
            try
            {
                var getRequest = new GetObjectMetadataRequest
                {
                    BucketName = BUCKET_NAME,
                    Key = key
                };

                var response = await _s3Client.GetObjectMetadataAsync(getRequest);

                if (response.Metadata.Keys.Contains("x-amz-meta-cid"))
                {
                    var cid = response.Metadata["x-amz-meta-cid"];
                    return $"https://ipfs.filebase.io/ipfs/{cid}";
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
        }
    }
}
