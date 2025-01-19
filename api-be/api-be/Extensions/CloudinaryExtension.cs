using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using api_be.Validators;

namespace api_be.Extensions
{
    public static class CloudinaryExtension
    {


        public static async Task<string> UploadImageAsync(IFormFile file, Cloudinary _cloudinary)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            // Lấy tên tệp từ file
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileExtension = Path.GetExtension(file.FileName);

            // Tạo PublicId từ tên file (tránh ký tự đặc biệt)
            var safeFileName = $"{Guid.NewGuid()}_{originalFileName}"
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("!", "")
                .Replace("@", "")
                .Replace("#", "")
                .Replace("$", "")
                .Replace("%", "")
                .Replace("^", "")
                .Replace("&", "")
                .Replace("*", "")
                .Replace("(", "")
                .Replace(")", "");
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "supermarket",
                    PublicId = safeFileName // Tên tệp tùy chỉnh

                };

                var uploadResult =  await _cloudinary.UploadAsync(uploadParams);
                return uploadResult?.SecureUrl?.AbsoluteUri ?? string.Empty;
            }
        }
            public static  async Task<ImageUploadResult?> UploadImageToCloudinary(string imageBase64, Cloudinary _cloudinary)
        {
            //if (!ValidatorCustom.BeValidImage(imageBase64))
            //{
            //    throw new ArgumentException("The provided image is not valid.");
            //}

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("image", new System.IO.MemoryStream(Convert.FromBase64String(imageBase64))),
                Transformation = new Transformation().Crop("fill").Width(500).Height(500)
            };

            return  _cloudinary.Upload(uploadParams);
        }
        //public static async Task<ImageUploadResult?> UploadImageToCloudinary(IFormFile? image, Cloudinary _cloudinary)
        //{
        //    if (image == null) return null;

        //    using var stream = image.OpenReadStream();
        //    var uploadParams = new ImageUploadParams
        //    {
        //        File = new FileDescription(image.FileName, stream),
        //        Transformation = new Transformation().Crop("fill").Gravity("face").Width(500).Height(500)
        //    };
        //    return  _cloudinary.Upload(uploadParams);
        //}
    }
}
